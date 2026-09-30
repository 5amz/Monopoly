namespace Monopoly.Administracion;

/// <summary>Contrato que el módulo de turnos debe ofrecer al servidor.</summary>
public interface IValidadorTurnos
{
    /// <summary>Indica si el jugador puede ejecutar la acción en este momento.</summary>
    bool EsTurnoActual(string idJugador);
}

/// <summary>
/// Contrato mínimo para recibir el total generado por el dado electrónico.
/// El coordinador consume cada resultado una sola vez.
/// </summary>
public interface IProveedorDados
{
    bool IntentarConsumirResultado(out ResultadoDados resultado);
}

/// <summary>Resultado total validado de los dados físicos.</summary>
public sealed class ResultadoDados
{
    public int Total { get; }

    public ResultadoDados(int total)
    {
        if (total is < 2 or > 12)
            throw new ArgumentOutOfRangeException(nameof(total));

        Total = total;
    }
}

/// <summary>Contrato para delegar acciones al módulo de tablero, turnos y dados.</summary>
public interface IAccionesJuego
{
    ResultadoAccionJuego TirarDados(string idJugador);
    ResultadoAccionJuego ComprarPropiedad(string idJugador);
    ResultadoAccionJuego NoComprarPropiedad(string idJugador);
    ResultadoAccionJuego TerminarTurno(string idJugador);
}

/// <summary>Contrato para crear la representación espacial al registrar un jugador.</summary>
public interface IRegistroJugadoresJuego
{
    ResultadoAccionJuego RegistrarJugadorEnJuego(string idJugador, string nombre);
}

/// <summary>Contrato para retirar un jugador de las estructuras del juego.</summary>
public interface IEliminacionJugadoresJuego
{
    ResultadoAccionJuego EliminarJugadorDelJuego(string idJugador);
}

/// <summary>Resultado que un módulo del juego devuelve al servidor.</summary>
public sealed class ResultadoAccionJuego
{
    public bool FueExitosa { get; }
    public string Mensaje { get; }
    public string Datos { get; }

    public ResultadoAccionJuego(bool fueExitosa, string mensaje, string datos = "")
    {
        FueExitosa = fueExitosa;
        Mensaje = mensaje;
        Datos = datos;
    }
}
