namespace Monopoly.Administracion;

/// <summary>Contrato que el módulo de turnos debe ofrecer al servidor.</summary>
public interface IValidadorTurnos
{
    /// <summary>Indica si el jugador puede ejecutar la acción en este momento.</summary>
    bool EsTurnoActual(string idJugador);
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
