namespace Monopoly.Administracion;

/// <summary>Representa una línea ya validada en su formato básico.</summary>
public sealed class SolicitudProtocolo
{
    public ComandoProtocolo Comando { get; }
    public string IdJugador { get; }
    public string NombreJugador { get; }
    public decimal SaldoInicial { get; }

    private SolicitudProtocolo(ComandoProtocolo comando, string idJugador, string nombreJugador, decimal saldoInicial)
    {
        Comando = comando;
        IdJugador = idJugador;
        NombreJugador = nombreJugador;
        SaldoInicial = saldoInicial;
    }

    /// <summary>Crea la solicitud de identificación con los datos iniciales.</summary>
    public static SolicitudProtocolo Conectar(string idJugador, string nombreJugador, decimal saldoInicial)
        => new(ComandoProtocolo.CONECTAR, idJugador, nombreJugador, saldoInicial);

    /// <summary>Crea una solicitud que no necesita parámetros adicionales.</summary>
    public static SolicitudProtocolo CrearAccion(ComandoProtocolo comando)
        => new(comando, string.Empty, string.Empty, 0);
}
