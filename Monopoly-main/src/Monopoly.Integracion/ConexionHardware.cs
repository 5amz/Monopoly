using Monopoly.Hardware;

namespace Monopoly.Integracion;


public sealed class ConexionHardware : IDisposable
{
    private readonly LectorHardwareSerial lector;

// Crea el objeto.
    public ConexionHardware(string nombre, Action<EventoHardware> recibirEvento, Action<string> informarError)
    {
        lector = new LectorHardwareSerial(nombre);
        lector.EventoRecibido += recibirEvento;
        lector.ErrorLectura += informarError;
    }

// Ejecuta Iniciar.
    public void Iniciar() => lector.Iniciar();
// Ejecuta Dispose.
    public void Dispose() => lector.Dispose();
}
