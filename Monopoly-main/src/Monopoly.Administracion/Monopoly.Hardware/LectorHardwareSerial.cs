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

// Crea el objeto.
    public LectorHardwareSerial(string puerto)
    {
        _serial = new SerialPort(puerto, 115200)
        {
            NewLine = "\n",
            ReadTimeout = 500,
            
            DtrEnable = true
        };
    }

// Ejecuta Iniciar.
    public void Iniciar()
    {
        if (_serial.IsOpen) return;
        _detener = false;
        _serial.Open();
        _serial.DiscardInBuffer();
        Task.Run(Escuchar);
    }

// Ejecuta Escuchar.
    private void Escuchar()
    {
        
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

    
// Ejecuta ProcesarLineaRecibida.
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
            
            
            
            
            
            ErrorLectura?.Invoke("Evento de hardware rechazado: " + ex.Message);
        }

        return true;
    }

    
// Ejecuta IntentarConsumirResultado.
    public bool IntentarConsumirResultado(out ResultadoDados resultado)
    {
        lock (_bloqueoDados)
        {
            resultado = _resultadoPendiente!;
            _resultadoPendiente = null;
            return resultado is not null;
        }
    }

// Ejecuta Dispose.
    public void Dispose()
    {
        _detener = true;
        _serial.Dispose();
    }
}
