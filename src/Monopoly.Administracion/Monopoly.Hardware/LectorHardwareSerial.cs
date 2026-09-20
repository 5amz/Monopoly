using System.Diagnostics.Contracts;
using System.IO.Ports;

namespace Monopoly.Hardware;

public sealed class LectorHarwareSerial : IDisposable
{
    private readonly SerialPort _serial;

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
                
                ProcesarLinea(linea);
            }
            catch
            {
            }
        }
    }

    private void ProcesarLinea(
        string linea)
    {
        string[] partes =
            linea.Split('|');
        
        if (partes.Length != 2)
            return;
        
        if (partes[0] == "RFID")
        {
            EventoRecibido?.Invoke(
                new EventoHardware(
                    TipoEventoHardware.Dado,
                    partes[1]));
        }
    }

    public void Dispose()
    {
        if (_serial.IsOpen)
            _serial.Close();
    }
}