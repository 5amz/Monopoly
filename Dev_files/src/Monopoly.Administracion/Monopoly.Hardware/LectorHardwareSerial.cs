#nullable enable

using System.IO.Ports;
using Monopoly.Administracion;

namespace Monopoly.Hardware;

public sealed class LectorHardwareSerial : IDisposable, IProveedorDados
{
    private readonly SerialPort _serial;
    private readonly object _bloqueoDados = new();
    private ResultadoDados? _resultadoPendiente;

    public event Action<EventoHardware>? EventoRecibido;

    public LectorHardwareSerial(
        string puerto)
    {
        _serial = new SerialPort(
            puerto,
            115200);
        
        _serial.NewLine = "\n";

    }

    public void Iniciar()
    {
        _serial.Open();

        Task.Run(Escuchar);
    }

    private void Escuchar()
    {
        while (_serial.IsOpen)
        {
            try
            {
                string linea =
                    _serial.ReadLine().Trim();
                
                ProcesarLineaRecibida(linea);
            }
            catch
            {
            }
        }
    }

    internal void ProcesarLineaRecibida(string linea)
    {
        if (string.IsNullOrWhiteSpace(linea))
            return;

        string[] partes = linea.Split('|');
        if (partes.Length == 2 && partes[0].Equals("RFID", StringComparison.OrdinalIgnoreCase))
        {
            EventoRecibido?.Invoke(
                new EventoHardware(
                    TipoEventoHardware.RFID,
                    partes[1]));

            return;
        }

        if (partes.Length == 2
            && partes[0].Equals("DADO", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(partes[1], out int total)
            && total is >= 2 and <= 12)
        {
            var resultado = new ResultadoDados(total);
            lock (_bloqueoDados)
                _resultadoPendiente = resultado;

            EventoRecibido?.Invoke(
                new EventoHardware(
                    TipoEventoHardware.Dado,
                    total.ToString()));
        }
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
        if (_serial.IsOpen)
            _serial.Close();
    }
}
