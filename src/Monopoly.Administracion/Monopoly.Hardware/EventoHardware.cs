namespace Monopoly.Hardware;

public sealed class EventoHardware
{
    public TipoEventoHardware Tipo {get;}

    public string Valor {get;}

    public EventoHardware(
        TipoEventoHardware tipo,
        string valor)
    {
        Tipo = tipo;
        Valor = valor;
    }
}
