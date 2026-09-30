namespace Monopoly.Administracion;

/// <summary>Estados oficiales posibles de una partida.</summary>
public enum EstadoPartida
{
    EsperandoJugadores,
    EnCurso,
    Finalizada
}

/// <summary>
/// Estado oficial compartido por el servidor TCP y el coordinador. El protocolo
/// y las sesiones viven en ServidorIntegrado; las operaciones económicas aquí.
/// </summary>
public sealed class ServidorJuego
{
    private IValidadorTurnos _turnos;
    private IAccionesJuego _accionesJuego;
    private IRegistroJugadoresJuego _registroJugadoresJuego;
    private IEliminacionJugadoresJuego _eliminacionJugadoresJuego;

    public Banco Banco { get; }
    public EstadoPartida Estado { get; private set; }
    public decimal SaldoInicialJugadores { get; }

    public ServidorJuego(
        decimal saldoInicialJugadores,
        int maximoJugadores = 4,
        IValidadorTurnos turnos = null,
        IAccionesJuego accionesJuego = null,
        IRegistroJugadoresJuego registroJugadoresJuego = null,
        IEliminacionJugadoresJuego eliminacionJugadoresJuego = null)
    {
        if (saldoInicialJugadores < 0)
            throw new ArgumentOutOfRangeException(nameof(saldoInicialJugadores));

        Banco = new Banco(maximoJugadores);
        SaldoInicialJugadores = saldoInicialJugadores;
        Estado = EstadoPartida.EsperandoJugadores;
        ConfigurarModulos(turnos, accionesJuego, registroJugadoresJuego, eliminacionJugadoresJuego);
    }

    /// <summary>El cliente nunca elige el saldo inicial.</summary>
    public ResultadoOperacion RegistrarJugador(string id, string nombre)
    {
        if (Estado != EstadoPartida.EsperandoJugadores)
            return ResultadoOperacion.Error("No se pueden registrar jugadores después de iniciar la partida.");

        ResultadoOperacion resultado = Banco.RegistrarJugador(id, nombre, SaldoInicialJugadores);
        if (!resultado.FueExitosa || _registroJugadoresJuego is null)
            return resultado;

        ResultadoAccionJuego registroTablero = _registroJugadoresJuego.RegistrarJugadorEnJuego(id.Trim(), nombre.Trim());
        if (registroTablero.FueExitosa)
            return resultado;
        Banco.CancelarRegistroIncompleto(id.Trim());
        return ResultadoOperacion.Error($"No se pudo sincronizar el registro con el tablero: {registroTablero.Mensaje}");
    }

    public void ConfigurarModulos(
        IValidadorTurnos turnos,
        IAccionesJuego accionesJuego,
        IRegistroJugadoresJuego registroJugadoresJuego,
        IEliminacionJugadoresJuego eliminacionJugadoresJuego)
    {
        _turnos = turnos;
        _accionesJuego = accionesJuego;
        _registroJugadoresJuego = registroJugadoresJuego;
        _eliminacionJugadoresJuego = eliminacionJugadoresJuego;
    }

    public ResultadoOperacion ActualizarPosicionDesdeTablero(string idJugador, int posicion)
    {
        if (posicion < 0)
            return ResultadoOperacion.Error("La posición no puede ser negativa.");
        Jugador jugador = Banco.ConsultarJugador(idJugador);
        if (jugador is null || !jugador.EstaActivo)
            return ResultadoOperacion.Error("El jugador no está registrado o está eliminado.");
        jugador.ActualizarPosicionDesdeServidor(posicion);
        return ResultadoOperacion.Exito("Posición oficial actualizada.", jugador.Saldo, jugador.Saldo);
    }

    /// <summary>Reconstruye el índice del jugador desde la única fuente de dueños: Tablero.</summary>
    public ResultadoOperacion SincronizarPropiedadesDesdeTablero(string idJugador, global::Monopoly.Tablero tablero)
    {
        Jugador jugador = Banco.ConsultarJugador(idJugador);
        if (jugador is null || tablero is null)
            return ResultadoOperacion.Error("El jugador o el tablero no existe.");
        jugador.Propiedades.VaciarDesdeTablero();
        for (int i = 0; i < tablero.Cantidad; i++)
        {
            if (tablero.ObtenerCasilla(i) is global::Monopoly.Propiedad propiedad
                && string.Equals(propiedad.Propietario?.IdJugador, jugador.Id, StringComparison.OrdinalIgnoreCase))
                jugador.Propiedades.AgregarDesdeTablero(propiedad);
        }
        return ResultadoOperacion.Exito("Índice de propiedades sincronizado.", jugador.Saldo, jugador.Saldo);
    }

    /// <summary>Compra voluntaria: saldo insuficiente rechaza, nunca elimina.</summary>
    public ResultadoOperacion ProcesarCompraPropiedad(string idJugador, decimal precio, string nombrePropiedad, int numeroTurno)
    {
        if (Estado != EstadoPartida.EnCurso)
            return ResultadoOperacion.Error("La partida no está en curso.");
        return Banco.Cobrar(idJugador, precio, $"Compra de propiedad: {nombrePropiedad}", TipoTransaccion.CompraPropiedad, numeroTurno);
    }

    /// <summary>Los demás cobros son obligatorios; la insolvencia elimina al deudor.</summary>
    public ResultadoOperacion ProcesarCobro(string idJugador, decimal monto, string motivo, TipoTransaccion tipo, int numeroTurno)
    {
        if (Estado != EstadoPartida.EnCurso)
            return ResultadoOperacion.Error("La partida no está en curso.");
        ResultadoOperacion cobro = Banco.Cobrar(idJugador, monto, motivo, tipo, numeroTurno);
        return tipo == TipoTransaccion.CompraPropiedad ? cobro : AplicarEliminacionPorInsolvencia(idJugador, cobro);
    }

    public ResultadoOperacion ProcesarAbono(string idJugador, decimal monto, string motivo, TipoTransaccion tipo, int numeroTurno)
    {
        if (Estado != EstadoPartida.EnCurso)
            return ResultadoOperacion.Error("La partida no está en curso.");
        return Banco.Abonar(idJugador, monto, motivo, tipo, numeroTurno);
    }

    public ResultadoOperacion ProcesarPagoAlquiler(string idJugadorOrigen, string idJugadorDestino, decimal alquiler, string nombrePropiedad, int numeroTurno)
        => ProcesarTransferencia(idJugadorOrigen, idJugadorDestino, alquiler, $"Pago de alquiler: {nombrePropiedad}", TipoTransaccion.PagoAlquiler, numeroTurno);

    public ResultadoOperacion ProcesarTransferencia(string idJugadorOrigen, string idJugadorDestino, decimal monto, string motivo, TipoTransaccion tipo, int numeroTurno)
    {
        if (Estado != EstadoPartida.EnCurso)
            return ResultadoOperacion.Error("La partida no está en curso.");
        ResultadoOperacion pago = Banco.Transferir(idJugadorOrigen, idJugadorDestino, monto, motivo, tipo, numeroTurno);
        return AplicarEliminacionPorInsolvencia(idJugadorOrigen, pago);
    }

    public ResultadoOperacion ProcesarGananciaEvento(string idJugador, decimal monto, string descripcion, int numeroTurno)
        => ProcesarAbono(idJugador, monto, descripcion, TipoTransaccion.GananciaPorEvento, numeroTurno);

    public ResultadoOperacion ProcesarPerdidaEvento(string idJugador, decimal monto, string descripcion, int numeroTurno)
        => ProcesarCobro(idJugador, monto, descripcion, TipoTransaccion.PerdidaPorEvento, numeroTurno);

    public ResultadoOperacion ProcesarPremioInicio(string idJugador, decimal monto, int numeroTurno)
        => ProcesarAbono(idJugador, monto, "Premio por pasar por inicio.", TipoTransaccion.PremioPorPasarInicio, numeroTurno);

    public void MarcarPartidaIniciada()
    {
        if (Estado != EstadoPartida.EsperandoJugadores)
            throw new InvalidOperationException("La partida ya inició o finalizó.");
        if (Banco.CantidadJugadores != 4 || Banco.CantidadJugadoresActivos != 4)
            throw new InvalidOperationException("La partida requiere exactamente cuatro jugadores activos.");
        if (_turnos is null || _accionesJuego is null)
            throw new InvalidOperationException("Debe conectar los módulos de tablero y turnos antes de iniciar.");
        Estado = EstadoPartida.EnCurso;
    }

    public void MarcarPartidaFinalizada() => Estado = EstadoPartida.Finalizada;
    public void ExportarTransacciones(string rutaArchivo) => Banco.Historial.ExportarATxt(rutaArchivo);
    public string GenerarResumenEstado() => $"Estado={Estado}#Jugadores={Banco.GenerarResumenJugadores()}";

    /// <summary>Retiro idempotente. Una reconexión nunca llama este método ni reactiva jugadores.</summary>
    public ResultadoOperacion EliminarJugador(string idJugador, string motivo = "Abandono administrativo.")
    {
        Jugador jugador = Banco.ConsultarJugador(idJugador);
        if (jugador is null)
            return ResultadoOperacion.Error("El jugador no está registrado.");
        if (!jugador.EstaActivo)
            return ResultadoOperacion.Exito("El jugador ya estaba eliminado.", jugador.Saldo, jugador.Saldo);
        jugador.CambiarEstadoActivoDesdeServidor(false);
        ResultadoAccionJuego eliminacion = _eliminacionJugadoresJuego?.EliminarJugadorDelJuego(idJugador);
        jugador.Propiedades.VaciarDesdeTablero();
        if (Estado == EstadoPartida.EnCurso && Banco.CantidadJugadoresActivos <= 1)
            Estado = EstadoPartida.Finalizada;
        if (eliminacion is not null && !eliminacion.FueExitosa)
            return ResultadoOperacion.Error($"Jugador eliminado, pero falló la sincronización: {eliminacion.Mensaje}", jugador.Saldo);
        return ResultadoOperacion.Exito(motivo, jugador.Saldo, jugador.Saldo);
    }

    private ResultadoOperacion AplicarEliminacionPorInsolvencia(string idJugador, ResultadoOperacion resultado)
    {
        if (resultado.FueExitosa || !resultado.FueRechazadaPorFondosInsuficientes)
            return resultado;
        EliminarJugador(idJugador, "El jugador fue eliminado por insolvencia.");
        return ResultadoOperacion.Error($"{resultado.Mensaje} El jugador fue eliminado por insolvencia.", resultado.SaldoActual, true);
    }
}
