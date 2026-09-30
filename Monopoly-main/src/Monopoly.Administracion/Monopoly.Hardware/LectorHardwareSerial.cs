#nullable enable
using System.IO.Ports;
using Monopoly.Administracion;

namespace Monopoly.Hardware;

public sealed class LectorHardwareSerial : IDisposable, IProveedorDados
{
    private readonly SerialPort _serial;
    private readonly object _bloqueoDados = new();
    private ResultadoDados? _resultadoPendiente;
    private volatile bool _detener;

    public event Action<EventoHardware>? EventoRecibido;
    public event Action<string>? ErrorLectura;

    public LectorHardwareSerial(string puerto)
    {
        _serial = new SerialPort(puerto, 115200)
        {
            NewLine = "\n",
            ReadTimeout = 500,
            // MicroPython reconoce al anfitrión USB CDC mediante DTR y vacía su búfer al conectar.
            DtrEnable = true
        };
    }

    public void Iniciar()
    {
        if (_serial.IsOpen) return;
        _detener = false;
        _serial.Open();
        _serial.DiscardInBuffer();
        Task.Run(Escuchar);
    }

    private void Escuchar()
    {
        // Lectura acotada: una línea incompleta o corrupta no crece indefinidamente.
        var linea = new System.Text.StringBuilder();
        bool descartar = false;
        while (!_detener)
        {
            try
            {
                int caracter = _serial.ReadChar();
                if (caracter == '\n')
                {
                    if (!descartar) ProcesarLineaRecibida(linea.ToString().TrimEnd('\r'));
                    linea.Clear();
                    descartar = false;
                }
                else if (!descartar)
                {
                    if (linea.Length >= 80) { descartar = true; linea.Clear(); }
                    else linea.Append((char)caracter);
                }
            }
            catch (TimeoutException) { }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                if (!_detener) ErrorLectura?.Invoke(ex.Message);
                break;
            }
        }
    }

    /// <summary>Permite probar el mismo límite serial sin abrir COM1 ni otro puerto físico.</summary>
    public bool ProcesarLineaRecibida(string linea)
    {
        if (!AnalizadorHardware.IntentarAnalizar(linea, out EventoHardware? evento)) return false;
        if (evento!.ResultadoDados is not null)
            lock (_bloqueoDados) _resultadoPendiente = evento.ResultadoDados;

        try
        {
            EventoRecibido?.Invoke(evento);
        }
        catch (Exception ex)
        {
            // Un evento válido puede disparar un fallo inesperado más adelante (reglas del
            // juego, difusión a los clientes, etc.). Antes, esa excepción escapaba de aquí y
            // tumbaba para siempre el bucle de Escuchar(): el Pico quedaba mudo el resto de la
            // partida sin ningún aviso, aunque el proceso siguiera respondiendo con normalidad.
            // Se reporta y se sigue leyendo; un evento puntual no debe apagar el puerto entero.
            ErrorLectura?.Invoke("Evento de hardware rechazado: " + ex.Message);
        }

        return true;
    }

    /// <summary>Entrega el último resultado físico y lo elimina para impedir reutilizarlo.</summary>
    public bool IntentarConsumirResultado(out ResultadoDados resultado)
    {
        lock (_bloqueoDados)
        {
            resultado = _resultadoPendiente!;
            _resultadoPendiente = null;
            return resultado is not null;
        }
    }

    public void Dispose()
    {
        _detener = true;
        _serial.Dispose();
    }
}
