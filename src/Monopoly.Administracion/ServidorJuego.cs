namespace Monopoly.Administracion;

/// <summary>
/// Conserva el estado oficial y traduce solicitudes válidas a llamadas de Banco
/// o de los módulos que se conectarán posteriormente.
/// </summary>
public sealed class ServidorJuego
{
    private readonly IValidadorTurnos _turnos;
    private readonly IAccionesJuego _accionesJuego;

    public Banco Banco { get; }
    public EstadoPartida Estado { get; private set; }

    /// <summary>
    /// Crea el estado oficial. Los contratos son opcionales mientras los demás
    /// módulos no estén integrados; en ese caso el servidor devuelve un error claro.
    /// </summary>
    public ServidorJuego(
        int maximoJugadores = 4,
        IValidadorTurnos turnos = null,
        IAccionesJuego accionesJuego = null)
    {
        Banco = new Banco(maximoJugadores);
        Estado = EstadoPartida.EsperandoJugadores;
        _turnos = turnos;
        _accionesJuego = accionesJuego;
    }

    /// <summary>Registra un jugador únicamente antes de iniciar la partida.</summary>
    public ResultadoOperacion RegistrarJugador(string id, string nombre, decimal saldoInicial)
    {
        if (Estado != EstadoPartida.EsperandoJugadores)
            return ResultadoOperacion.Error("No se pueden registrar jugadores después de iniciar la partida.");

        return Banco.RegistrarJugador(id, nombre, saldoInicial);
    }

    /// <summary>Procesa un cobro oficial; el cliente nunca modifica el saldo.</summary>
    public ResultadoOperacion ProcesarCobro(
        string idJugador,
        decimal monto,
        string motivo,
        TipoTransaccion tipo,
        int numeroTurno)
    {
        if (Estado == EstadoPartida.Finalizada)
            return ResultadoOperacion.Error("La partida ya finalizó.");

        return Banco.Cobrar(idJugador, monto, motivo, tipo, numeroTurno);
    }

    /// <summary>Procesa un abono oficial; el cliente nunca modifica el saldo.</summary>
    public ResultadoOperacion ProcesarAbono(
        string idJugador,
        decimal monto,
        string motivo,
        TipoTransaccion tipo,
        int numeroTurno)
    {
        if (Estado == EstadoPartida.Finalizada)
            return ResultadoOperacion.Error("La partida ya finalizó.");

        return Banco.Abonar(idJugador, monto, motivo, tipo, numeroTurno);
    }

    /// <summary>
    /// Identifica una conexión. Si el jugador existe, permite reconexión usando
    /// el mismo ID y nombre; si no existe, intenta registrarlo antes de iniciar.
    /// </summary>
    public RespuestaProtocolo ConectarJugador(SolicitudProtocolo solicitud)
    {
        Jugador jugadorExistente = Banco.ConsultarJugador(solicitud.IdJugador);
        if (jugadorExistente is not null)
        {
            if (!jugadorExistente.Nombre.Equals(solicitud.NombreJugador, StringComparison.OrdinalIgnoreCase))
                return RespuestaProtocolo.Error("IDENTIFICACION_INVALIDA", "El nombre no coincide con el jugador registrado.");

            return RespuestaProtocolo.Exito(ComandoProtocolo.CONECTAR, "Jugador identificado.", jugadorExistente.Id);
        }

        ResultadoOperacion resultado = RegistrarJugador(solicitud.IdJugador, solicitud.NombreJugador, solicitud.SaldoInicial);
        return resultado.FueExitosa
            ? RespuestaProtocolo.Exito(ComandoProtocolo.CONECTAR, resultado.Mensaje, solicitud.IdJugador)
            : RespuestaProtocolo.Error("CONEXION_RECHAZADA", resultado.Mensaje);
    }

    /// <summary>
    /// Valida y procesa una solicitud de un jugador ya identificado. Las acciones
    /// de tablero y turnos se delegan mediante interfaces, no se implementan aquí.
    /// </summary>
    public RespuestaProtocolo ProcesarSolicitud(string idJugador, SolicitudProtocolo solicitud)
    {
        Jugador jugador = Banco.ConsultarJugador(idJugador);
        if (jugador is null)
            return RespuestaProtocolo.Error("JUGADOR_NO_EXISTE", "El jugador identificado ya no existe en el estado oficial.");

        if (!jugador.EstaActivo)
            return RespuestaProtocolo.Error("JUGADOR_INACTIVO", "El jugador no puede realizar acciones.");

        return solicitud.Comando switch
        {
            ComandoProtocolo.CONSULTAR_ESTADO => ConsultarEstado(jugador),
            ComandoProtocolo.CONSULTAR_TRANSACCIONES => ConsultarTransacciones(idJugador),
            ComandoProtocolo.TIRAR_DADOS => ProcesarAccionDeJuego(idJugador, solicitud.Comando),
            ComandoProtocolo.COMPRAR_PROPIEDAD => ProcesarAccionDeJuego(idJugador, solicitud.Comando),
            ComandoProtocolo.NO_COMPRAR => ProcesarAccionDeJuego(idJugador, solicitud.Comando),
            ComandoProtocolo.TERMINAR_TURNO => ProcesarAccionDeJuego(idJugador, solicitud.Comando),
            ComandoProtocolo.CONECTAR => RespuestaProtocolo.Error("SOLICITUD_INVALIDA", "CONECTAR solo es válido al inicio de una conexión."),
            _ => RespuestaProtocolo.Error("COMANDO_DESCONOCIDO", "El comando no está respaldado.")
        };
    }

    /// <summary>Cambia la partida a EnCurso si existe al menos un jugador.</summary>
    public void MarcarPartidaIniciada()
    {
        if (Banco.CantidadJugadores == 0)
            throw new InvalidOperationException("No se puede iniciar una partida sin jugadores.");

        Estado = EstadoPartida.EnCurso;
    }

    /// <summary>Marca la partida como finalizada.</summary>
    public void MarcarPartidaFinalizada()
    {
        Estado = EstadoPartida.Finalizada;
    }

    /// <summary>Exporta el historial oficial para el entregable TXT.</summary>
    public void ExportarTransacciones(string rutaArchivo)
    {
        Banco.Historial.ExportarATxt(rutaArchivo);
    }

    /// <summary>Genera el estado resumido que el servidor envía a los clientes.</summary>
    public string GenerarResumenEstado()
    {
        return $"Estado={Estado}#Jugadores={Banco.GenerarResumenJugadores()}";
    }

    private RespuestaProtocolo ConsultarEstado(Jugador jugador)
    {
        string datos = string.Join(';', jugador.Id, jugador.Nombre, jugador.Saldo, jugador.PosicionActual, jugador.EstaActivo, Estado);
        return RespuestaProtocolo.Exito(ComandoProtocolo.CONSULTAR_ESTADO, "Estado consultado.", datos);
    }

    private RespuestaProtocolo ConsultarTransacciones(string idJugador)
    {
        return RespuestaProtocolo.Exito(
            ComandoProtocolo.CONSULTAR_TRANSACCIONES,
            "Transacciones consultadas.",
            Banco.Historial.GenerarReportePorJugador(idJugador));
    }

    private RespuestaProtocolo ProcesarAccionDeJuego(string idJugador, ComandoProtocolo comando)
    {
        if (Estado != EstadoPartida.EnCurso)
            return RespuestaProtocolo.Error("PARTIDA_NO_INICIADA", "La acción solo es válida durante una partida en curso.");

        if (_turnos is null || _accionesJuego is null)
            return RespuestaProtocolo.Error("MODULO_NO_INTEGRADO", "Los módulos de turnos y juego aún no están conectados al servidor.");

        if (!_turnos.EsTurnoActual(idJugador))
            return RespuestaProtocolo.Error("TURNO_INVALIDO", "No es el turno del jugador identificado.");

        ResultadoAccionJuego resultado = comando switch
        {
            ComandoProtocolo.TIRAR_DADOS => _accionesJuego.TirarDados(idJugador),
            ComandoProtocolo.COMPRAR_PROPIEDAD => _accionesJuego.ComprarPropiedad(idJugador),
            ComandoProtocolo.NO_COMPRAR => _accionesJuego.NoComprarPropiedad(idJugador),
            ComandoProtocolo.TERMINAR_TURNO => _accionesJuego.TerminarTurno(idJugador),
            _ => new ResultadoAccionJuego(false, "La acción no puede ser procesada.")
        };

        return resultado.FueExitosa
            ? RespuestaProtocolo.Exito(comando, resultado.Mensaje, resultado.Datos)
            : RespuestaProtocolo.Error("ACCION_RECHAZADA", resultado.Mensaje);
    }
}
