using Monopoly.Hardware;

namespace Monopoly.Integracion;

/// <summary>Adaptador del transporte serial oficial; no define un segundo analizador.</summary>
public sealed class ConexionHardware : IDisposable
{
    private readonly LectorHardwareSerial lector;

    public ConexionHardware(string nombre, Action<EventoHardware> recibirEvento, Action<string> informarError)
    {
        lector = new LectorHardwareSerial(nombre);
        lector.EventoRecibido += recibirEvento;
        lector.ErrorLectura += informarError;
    }

    public void Iniciar() => lector.Iniciar();
    public void Dispose() => lector.Dispose();
}
