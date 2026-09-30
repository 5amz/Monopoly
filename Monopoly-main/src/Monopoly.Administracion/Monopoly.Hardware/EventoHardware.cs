#nullable enable
using Monopoly.Administracion;

namespace Monopoly.Hardware;

public sealed class EventoHardware
{
    public TipoEventoHardware Tipo {get;}

    public string Valor {get;}
    public ResultadoDados? ResultadoDados { get; }

    public EventoHardware(
        TipoEventoHardware tipo,
        string valor,
        ResultadoDados? resultadoDados = null)
    {
        Tipo = tipo;
        Valor = valor;
        ResultadoDados = resultadoDados;
    }
}
