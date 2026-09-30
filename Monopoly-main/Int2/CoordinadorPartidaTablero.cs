using Monopoly.Administracion;

namespace Monopoly;

/// <summary>
/// Une las estructuras propias y el Banco. Toda acción del servidor de producción
/// pasa por este coordinador; el lector solo suministra identidad y dados.
/// </summary>
public sealed class CoordinadorPartidaTablero : IValidadorTurnos, IAccionesJuego, IRegistroJugadoresJuego, IEliminacionJugadoresJuego
{
    private readonly ServidorJuego _servidor;
    private readonly Tablero _tablero;
    private readonly ColaTurnos _turnos;
    private readonly JugadorTablero[] _jugadores;
    private readonly IProveedorDados _proveedorDados;
    private readonly MazoEventos _mazoEventos;
    private bool _dadosUsadosEnTurno;
    private string _idCompraPendiente;
    private string _idPagoPendiente;
    private Func<ResultadoAccionJuego> _pagoPendiente;
    private int _resolucionesEnCadena;

    public int NumeroTurnoActual { get; private set; } = 1;
    public bool DadosUsadosEnTurno => _dadosUsadosEnTurno;
    public bool HayCompraPendiente => _idCompraPendiente is not null;
    public bool HayPagoPendiente => _pagoPendiente is not null;
    public string IdJugadorPagoPendiente => _idPagoPendiente ?? string.Empty;
    public string DescripcionPagoPendiente { get; private set; } = string.Empty;
    public bool RequiereRfidParaPagos { get; }
    public CartaEvento UltimaCartaAplicada { get; private set; }
    public event Action<string, CartaEvento> CartaAplicada;

    public CoordinadorPartidaTablero(
        ServidorJuego servidor,
        Tablero tablero,
        ColaTurnos turnos,
        int maximoJugadores = 4,
        IProveedorDados proveedorDados = null,
        MazoEventos mazoEventos = null,
        bool requerirRfidParaPagos = true)
    {
        _servidor = servidor ?? throw new ArgumentNullException(nameof(servidor));
        _tablero = tablero ?? throw new ArgumentNullException(nameof(tablero));
        _turnos = turnos ?? throw new ArgumentNullException(nameof(turnos));
        _proveedorDados = proveedorDados;
        _mazoEventos = mazoEventos;
        RequiereRfidParaPagos = requerirRfidParaPagos;
        if (maximoJugadores <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximoJugadores));
        _jugadores = new JugadorTablero[maximoJugadores];
    }

    public ResultadoAccionJuego RegistrarJugadorEnJuego(string idJugador, string nombre)
    {
        if (_tablero.Head is null)
            return new(false, "El tablero no tiene una casilla inicial configurada.");
        Jugador oficial = _servidor.Banco.ConsultarJugador(idJugador);
        if (oficial is null || !oficial.EstaActivo)
            return new(false, "El jugador no está registrado o fue eliminado.");
        JugadorTablero existente = ObtenerJugadorTablero(idJugador);
        if (existente is not null)
            return existente.Activo
                ? new(true, "El jugador ya estaba sincronizado; se conserva posición y turno.")
                : new(false, "No se puede reactivar a un jugador eliminado.");
        for (int i = 0; i < _jugadores.Length; i++)
        {
            if (_jugadores[i] is not null)
                continue;
            var jugador = new JugadorTablero(oficial.Id, oficial.Nombre, _tablero.Head);
            _jugadores[i] = jugador;
            _turnos.AgregarJugador(jugador);
            _servidor.ActualizarPosicionDesdeTablero(jugador.IdJugador, 0);
            return new(true, "Jugador agregado al tablero.", "0");
        }
        return new(false, "No hay espacio para otro jugador en el tablero.");
    }

    public bool EsTurnoActual(string idJugador)
    {
        NodoJugador actual = _turnos.ObtenerJugadorActual();
        return actual is not null && actual.Jugador.Activo
            && actual.IdJugador.Equals(idJugador, StringComparison.OrdinalIgnoreCase);
    }

    public ResultadoAccionJuego TirarDados(string idJugador)
    {
        ResultadoAccionJuego invalida = ValidarAccionTurno(idJugador);
        if (invalida is not null) return invalida;
        if (_dadosUsadosEnTurno)
            return new(false, "Los dados ya fueron utilizados durante este turno.");
        if (HayPagoPendiente)
            return new(false, "Debe identificar la tarjeta del pago pendiente.");
        if (_proveedorDados is null)
            return new(false, "No hay un proveedor de dados conectado.");
        if (!_proveedorDados.IntentarConsumirResultado(out ResultadoDados dados))
            return new(false, "No hay un resultado pendiente. Presione el botón del dado electrónico.");
        if (dados is null)
            return new(false, "El proveedor no entregó dados válidos.");

        _dadosUsadosEnTurno = true;
        UltimaCartaAplicada = null;
        _resolucionesEnCadena = 0;
        ResultadoAccionJuego movimiento = MoverJugador(idJugador, dados.Total);
        if (!movimiento.FueExitosa) return movimiento;
        JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
        ResultadoAccionJuego casilla = ProcesarCasillaActual(jugador);
        string datos = $"Total={dados.Total};{movimiento.Datos};Casilla={jugador.Posicion.Casilla.Nombre};{casilla.Datos}";
        return new(casilla.FueExitosa, $"Dados procesados. {casilla.Mensaje}", datos);
    }

    public ResultadoAccionJuego ComprarPropiedad(string idJugador)
    {
        ResultadoAccionJuego invalida = ValidarAccionTurno(idJugador);
        if (invalida is not null) return invalida;
        if (HayPagoPendiente)
            return new(false, "Ya hay un pago pendiente de confirmar con RFID.");
        if (!_dadosUsadosEnTurno || !EsCompraPendienteDe(idJugador))
            return new(false, "No hay una compra pendiente para este jugador.");
        JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
        if (jugador.Posicion.Casilla is not Propiedad propiedad || !_tablero.PuedeComprarPropiedad(jugador, propiedad))
            return new(false, "La propiedad no está disponible para este jugador.");
        return SolicitarPago(idJugador, $"Comprar {propiedad.Nombre} por {propiedad.Precio}", () =>
        {
            if (!_tablero.PuedeComprarPropiedad(jugador, propiedad))
                return new(false, "La propiedad ya tiene propietario o cambió la posición.");
            ResultadoOperacion pago = _servidor.ProcesarCompraPropiedad(idJugador, propiedad.Precio, propiedad.Nombre, NumeroTurnoActual);
            if (!pago.FueExitosa)
                return new(false, pago.Mensaje);
            // El servidor serializa las acciones. La validación y asignación no pueden intercalarse con otra compra.
            _tablero.AsignarPropiedad(jugador, propiedad);
            _servidor.SincronizarPropiedadesDesdeTablero(idJugador, _tablero);
            _idCompraPendiente = null;
            return new(true, "Propiedad comprada.", propiedad.Nombre);
        });
    }

    public ResultadoAccionJuego NoComprarPropiedad(string idJugador)
    {
        ResultadoAccionJuego invalida = ValidarAccionTurno(idJugador);
        if (invalida is not null) return invalida;
        if (HayPagoPendiente)
            return new(false, "Debe resolver el pago solicitado antes de otra decisión.");
        if (!_dadosUsadosEnTurno || !EsCompraPendienteDe(idJugador))
            return new(false, "No hay una compra pendiente para este jugador.");
        _idCompraPendiente = null;
        return new(true, "El jugador decidió no comprar la propiedad.");
    }

    public ResultadoAccionJuego PagarAlquilerActual(string idJugador)
    {
        ResultadoAccionJuego invalida = ValidarAccionTurno(idJugador);
        if (invalida is not null) return invalida;
        JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
        if (jugador.Posicion.Casilla is not Propiedad propiedad || !_tablero.PuedePagarAlquiler(jugador, propiedad))
            return new(false, "La propiedad no requiere pago de alquiler.");
        if (propiedad.Alquiler == 0)
            return new(true, "La propiedad tiene alquiler cero.");
        string propietario = propiedad.Propietario.IdJugador;
        return SolicitarPago(idJugador, $"Alquiler de {propiedad.Nombre}: {propiedad.Alquiler}", () =>
            ConvertirPago(_servidor.ProcesarPagoAlquiler(idJugador, propietario, propiedad.Alquiler, propiedad.Nombre, NumeroTurnoActual), idJugador));
    }

    /// <summary>Solo se llama desde la entrada serial del servidor, nunca desde un comando del cliente.</summary>
    public ResultadoAccionJuego ConfirmarPagoConRfid(string idJugador)
    {
        if (!HayPagoPendiente)
            return new(false, "No hay un pago pendiente para confirmar.");
        if (!string.Equals(idJugador, _idPagoPendiente, StringComparison.OrdinalIgnoreCase))
            return new(false, "La tarjeta no corresponde al jugador que debe pagar.");
        ResultadoAccionJuego invalida = ValidarAccionTurno(idJugador);
        if (invalida is not null) return invalida;
        Func<ResultadoAccionJuego> pago = _pagoPendiente;
        LimpiarPagoPendiente(); // Consume una vez, incluso si Banco rechaza o elimina al jugador.
        return pago();
    }

    public ResultadoAccionJuego EliminarJugadorDelJuego(string idJugador)
    {
        JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
        if (jugador is null)
            return new(false, "No existe una representación del jugador en el tablero.");
        if (!jugador.Activo)
            return new(true, "El jugador ya estaba retirado del tablero.");
        bool teniaTurno = EsTurnoActual(idJugador);
        _tablero.EliminarJugador(jugador);
        _turnos.EliminarJugador(idJugador);
        _servidor.SincronizarPropiedadesDesdeTablero(idJugador, _tablero);
        if (EsCompraPendienteDe(idJugador)) _idCompraPendiente = null;
        if (string.Equals(_idPagoPendiente, idJugador, StringComparison.OrdinalIgnoreCase)) LimpiarPagoPendiente();
        if (teniaTurno)
        {
            // EliminarJugador ya deja la cabeza en el sucesor. No llamar AvanzarTurno otra vez.
            _turnos.OmitirTurnosPerdidos();
            _dadosUsadosEnTurno = false;
            if (_servidor.Estado == EstadoPartida.EnCurso) NumeroTurnoActual++;
        }
        return new(true, "Jugador retirado de tablero y turnos.");
    }

    public ResultadoAccionJuego TerminarTurno(string idJugador)
    {
        ResultadoAccionJuego invalida = ValidarAccionTurno(idJugador);
        if (invalida is not null) return invalida;
        if (!_dadosUsadosEnTurno)
            return new(false, "El jugador debe tirar los dados antes de terminar el turno.");
        if (HayPagoPendiente)
            return new(false, "Debe confirmar el pago pendiente con su tarjeta RFID.");
        if (EsCompraPendienteDe(idJugador))
            return new(false, "Debe comprar la propiedad o rechazar la compra antes de terminar el turno.");
        NodoJugador siguiente = _turnos.AvanzarTurno(ObtenerJugadorTablero(idJugador));
        if (siguiente is null)
            return new(false, "No fue posible avanzar al siguiente turno.");
        NumeroTurnoActual++;
        _dadosUsadosEnTurno = false;
        return new(true, "Turno terminado.", siguiente.IdJugador);
    }

    /// <summary>Movimiento interno del coordinador; no se expone en el protocolo cliente.</summary>
    public ResultadoAccionJuego MoverJugador(string idJugador, int cantidadCasillas)
    {
        ResultadoAccionJuego invalida = ValidarAccionTurno(idJugador);
        if (invalida is not null) return invalida;
        JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
        ResultadoMovimientoTablero movimiento = _tablero.MoverJugador(jugador.Posicion, cantidadCasillas, jugador);
        if (movimiento.PosicionFinal is null)
            return new(false, "No fue posible mover al jugador.");
        _servidor.ActualizarPosicionDesdeTablero(idJugador, _tablero.ObtenerIndiceDeNodo(movimiento.PosicionFinal));
        ResultadoAccionJuego premio = PremiarPasosPorInicio(idJugador, movimiento.VecesPasoPorInicio);
        if (!premio.FueExitosa) return premio;
        return new(true, "Jugador movido.", $"Posicion={_tablero.ObtenerIndiceDeNodo(jugador.Posicion)}");
    }

    public ResultadoAccionJuego AplicarEvento(string idJugador, CartaEvento carta)
    {
        ResultadoAccionJuego invalida = ValidarAccionTurno(idJugador);
        if (invalida is not null) return invalida;
        if (HayPagoPendiente)
            return new(false, "Debe resolver primero el pago pendiente.");
        if (carta is null || carta.Valor < 0)
            return new(false, "La carta no contiene una configuración válida.");
        JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
        NodoCasilla posicionAnterior = jugador.Posicion;
        ResultadoEventoTablero evento = _tablero.EjecutarCartaEvento(carta, jugador);
        if (evento.TipoEvento == "INVALIDO")
            return new(false, "La carta tiene un tipo o destino desconocido.");
        UltimaCartaAplicada = carta;
        CartaAplicada?.Invoke(idJugador, carta);
        if (evento.TipoEvento == "PerderDinero" && evento.Monto > 0)
            return SolicitarPago(idJugador, carta.Descripcion, () => ConvertirPago(
                _servidor.ProcesarPerdidaEvento(idJugador, evento.Monto, carta.Descripcion, NumeroTurnoActual), idJugador));
        if (evento.TipoEvento == "GanarDinero" && evento.Monto > 0)
        {
            ResultadoOperacion ganancia = _servidor.ProcesarGananciaEvento(idJugador, evento.Monto, carta.Descripcion, NumeroTurnoActual);
            if (!ganancia.FueExitosa) return new(false, ganancia.Mensaje);
        }
        ResultadoAccionJuego premio = PremiarPasosPorInicio(idJugador, evento.VecesPasoPorInicio);
        if (!premio.FueExitosa) return premio;
        _servidor.ActualizarPosicionDesdeTablero(idJugador, _tablero.ObtenerIndiceDeNodo(jugador.Posicion));
        if (!ReferenceEquals(posicionAnterior, jugador.Posicion))
            return ProcesarCasillaActual(jugador);
        return new(true, "Evento aplicado.", evento.TipoEvento);
    }

    internal ResultadoAccionJuego ResolverPropiedad(JugadorTablero jugador, Propiedad propiedad)
    {
        if (propiedad.Disponible)
        {
            _idCompraPendiente = jugador.IdJugador;
            return new(true, $"La propiedad {propiedad.Nombre} está disponible.", $"AccionPendiente=COMPRAR_O_RECHAZAR;Precio={propiedad.Precio}");
        }
        if (string.Equals(propiedad.Propietario?.IdJugador, jugador.IdJugador, StringComparison.OrdinalIgnoreCase))
            return new(true, "El jugador cayó en una propiedad propia.");
        return PagarAlquilerActual(jugador.IdJugador);
    }

    internal ResultadoAccionJuego ResolverEvento(JugadorTablero jugador)
    {
        if (_mazoEventos is null || _mazoEventos.Cantidad == 0)
            return new(false, "El mazo de eventos no está configurado.");
        CartaEvento carta = _mazoEventos.ObtenerSiguienteCarta();
        ResultadoAccionJuego resultado = AplicarEvento(jugador.IdJugador, carta);
        return new(resultado.FueExitosa, $"Carta: {carta.Descripcion}. {resultado.Mensaje}", resultado.Datos);
    }

    internal ResultadoAccionJuego ResolverImpuesto(JugadorTablero jugador, CasillaEspecial casilla)
    {
        if (casilla.Monto < 0)
            return new(false, "El impuesto está configurado con un monto inválido.");
        if (casilla.Monto == 0)
            return new(true, "El impuesto está configurado en cero.");
        return SolicitarPago(jugador.IdJugador, $"{casilla.Nombre}: {casilla.Monto}", () => ConvertirPago(
            _servidor.ProcesarCobro(jugador.IdJugador, casilla.Monto, casilla.Nombre, TipoTransaccion.PagoAlBanco, NumeroTurnoActual), jugador.IdJugador));
    }

    public JugadorTablero ObtenerJugadorTablero(string idJugador)
    {
        if (string.IsNullOrWhiteSpace(idJugador)) return null;
        for (int i = 0; i < _jugadores.Length; i++)
            if (_jugadores[i] is not null && _jugadores[i].IdJugador.Equals(idJugador, StringComparison.OrdinalIgnoreCase))
                return _jugadores[i];
        return null;
    }

    public decimal CalcularPatrimonio(string idJugador)
    {
        Jugador oficial = _servidor.Banco.ConsultarJugador(idJugador);
        return oficial is null ? 0 : _tablero.CalcularPatrimonio(ObtenerJugadorTablero(idJugador), oficial.Saldo);
    }

    /// <summary>Empates: conserva el primero en orden de registro, decisión del equipo.</summary>
    public JugadorTablero ObtenerGanador()
        => _tablero.ObtenerGanador(_jugadores, id => _servidor.Banco.ConsultarSaldo(id) ?? 0);

    private ResultadoAccionJuego SolicitarPago(string idJugador, string descripcion, Func<ResultadoAccionJuego> pago)
    {
        if (HayPagoPendiente)
            return new(false, "Ya existe un pago pendiente.");
        if (!RequiereRfidParaPagos) return pago();
        _idPagoPendiente = idJugador;
        DescripcionPagoPendiente = descripcion;
        _pagoPendiente = pago;
        return new(true, $"Acerque la tarjeta RFID de {idJugador}: {descripcion}.", "AccionPendiente=RFID_PAGO");
    }

    private ResultadoAccionJuego ConvertirPago(ResultadoOperacion operacion, string idJugador)
    {
        if (operacion.FueExitosa) return new(true, operacion.Mensaje);
        Jugador oficial = _servidor.Banco.ConsultarJugador(idJugador);
        return oficial is not null && !oficial.EstaActivo
            ? new(true, operacion.Mensaje, "JugadorEliminado=True")
            : new(false, operacion.Mensaje);
    }

    private ResultadoAccionJuego ProcesarCasillaActual(JugadorTablero jugador)
    {
        if (++_resolucionesEnCadena > _tablero.Cantidad)
            return new(false, "La configuración de cartas produjo un ciclo de movimientos.");
        return jugador.Posicion?.Casilla is Casilla casilla
            ? casilla.Resolver(this, jugador)
            : new(false, "La posición no contiene una casilla válida.");
    }

    private ResultadoAccionJuego PremiarPasosPorInicio(string idJugador, int veces)
    {
        for (int i = 0; i < veces && _tablero.PremioInicio > 0; i++)
        {
            ResultadoOperacion premio = _servidor.ProcesarPremioInicio(idJugador, _tablero.PremioInicio, NumeroTurnoActual);
            if (!premio.FueExitosa) return new(false, premio.Mensaje);
        }
        return new(true, "Premios por Inicio procesados.");
    }

    private ResultadoAccionJuego ValidarAccionTurno(string idJugador)
    {
        if (_servidor.Estado != EstadoPartida.EnCurso)
            return new(false, "La partida no está en curso.");
        Jugador oficial = _servidor.Banco.ConsultarJugador(idJugador);
        if (oficial is null || !oficial.EstaActivo || !EsTurnoActual(idJugador))
            return new(false, "El jugador está eliminado o intenta actuar fuera de su turno.");
        return null;
    }

    private bool EsCompraPendienteDe(string idJugador)
        => string.Equals(_idCompraPendiente, idJugador, StringComparison.OrdinalIgnoreCase);

    private void LimpiarPagoPendiente()
    {
        _pagoPendiente = null;
        _idPagoPendiente = null;
        DescripcionPagoPendiente = string.Empty;
    }
}
