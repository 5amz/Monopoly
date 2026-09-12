namespace Monopoly.Administracion;

/// <summary>
/// Base del servidor: concentra el estado oficial y será el punto de entrada
/// de las solicitudes TCP. Aún no implementa sockets, tablero ni turnos.
/// </summary>
public sealed class ServidorJuego
{
    public Banco Banco { get; }
    public EstadoPartida Estado { get; private set; }

    /// <summary>Crea el estado oficial en espera de jugadores.</summary>
    public ServidorJuego(int maximoJugadores = 4)
    {
        Banco = new Banco(maximoJugadores);
        Estado = EstadoPartida.EsperandoJugadores;
    }

    // Un futuro manejador TCP llamará este método al recibir CONECTAR.
    /// <summary>Procesa el alta solicitada por un cliente antes de iniciar.</summary>
    public ResultadoOperacion RegistrarJugador(string id, string nombre, decimal saldoInicial)
    {
        if (Estado != EstadoPartida.EsperandoJugadores)
            return ResultadoOperacion.Error("No se pueden registrar jugadores después de iniciar la partida.");

        return Banco.RegistrarJugador(id, nombre, saldoInicial);
    }

    // Punto de entrada para acciones monetarias validadas por el servidor.
    /// <summary>Procesa un cobro oficial solicitado al servidor.</summary>
    public ResultadoOperacion ProcesarCobro(string idJugador, decimal monto, string motivo)
    {
        if (Estado == EstadoPartida.Finalizada)
            return ResultadoOperacion.Error("La partida ya finalizó.");

        return Banco.Cobrar(idJugador, monto, motivo);
    }

    /// <summary>Procesa un abono oficial solicitado al servidor.</summary>
    public ResultadoOperacion ProcesarAbono(string idJugador, decimal monto, string motivo)
    {
        if (Estado == EstadoPartida.Finalizada)
            return ResultadoOperacion.Error("La partida ya finalizó.");

        return Banco.Abonar(idJugador, monto, motivo);
    }

    // El módulo de turnos decidirá cuándo es válido cambiar a EnCurso.
    /// <summary>Cambia la partida a EnCurso si existe al menos un jugador.</summary>
    public void MarcarPartidaIniciada()
    {
        if (Banco.CantidadJugadores == 0)
            throw new InvalidOperationException("No se puede iniciar una partida sin jugadores.");

        Estado = EstadoPartida.EnCurso;
    }

    /// <summary>Marca la partida como finalizada y bloquea operaciones monetarias.</summary>
    public void MarcarPartidaFinalizada()
    {
        Estado = EstadoPartida.Finalizada;
    }
}
