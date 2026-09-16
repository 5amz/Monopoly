using Monopoly.Administracion;

namespace Monopoly
{
    /// <summary>
    /// Adaptador entre las estructuras del tablero y el servidor oficial.
    /// Valida la parte espacial y solicita a ServidorJuego toda operación de dinero.
    /// </summary>
    public sealed class CoordinadorPartidaTablero : IValidadorTurnos, IAccionesJuego, IRegistroJugadoresJuego, IEliminacionJugadoresJuego
    {
        private readonly ServidorJuego _servidor;
        private readonly Tablero _tablero;
        private readonly ColaTurnos _turnos;
        private readonly JugadorTablero[] _jugadores;

        public int NumeroTurnoActual { get; private set; } = 1;

        public CoordinadorPartidaTablero(
            ServidorJuego servidor,
            Tablero tablero,
            ColaTurnos turnos,
            int maximoJugadores = 4)
        {
            _servidor = servidor ?? throw new ArgumentNullException(nameof(servidor));
            _tablero = tablero ?? throw new ArgumentNullException(nameof(tablero));
            _turnos = turnos ?? throw new ArgumentNullException(nameof(turnos));

            if (maximoJugadores <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximoJugadores));

            _jugadores = new JugadorTablero[maximoJugadores];
        }

        /// <summary>Crea la representación espacial del jugador recién registrado por el servidor.</summary>
        public ResultadoAccionJuego RegistrarJugadorEnJuego(string idJugador, string nombre)
        {
            if (_tablero.Head is null)
                return new ResultadoAccionJuego(false, "El tablero no tiene una casilla inicial configurada.");

            JugadorTablero existente = ObtenerJugadorTablero(idJugador);
            if (existente is not null)
                return new ResultadoAccionJuego(true, "El jugador ya estaba sincronizado con el tablero.");

            for (int i = 0; i < _jugadores.Length; i++)
            {
                if (_jugadores[i] is not null)
                    continue;

                var jugador = new JugadorTablero(idJugador, nombre, _tablero.Head);
                _jugadores[i] = jugador;
                _turnos.AgregarJugador(jugador);
                _servidor.ActualizarPosicionDesdeTablero(idJugador, 0);
                return new ResultadoAccionJuego(true, "Jugador agregado al tablero.", "0");
            }

            return new ResultadoAccionJuego(false, "No hay espacio para otro jugador en el tablero.");
        }

        /// <summary>Indica si el ID coincide con el jugador al frente de la cola circular.</summary>
        public bool EsTurnoActual(string idJugador)
        {
            NodoJugador actual = _turnos.ObtenerJugadorActual();
            return actual is not null && actual.IdJugador.Equals(idJugador, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Reserva el comando hasta que el módulo de dado se conecte.</summary>
        public ResultadoAccionJuego TirarDados(string idJugador)
        {
            return new ResultadoAccionJuego(false, "El módulo de dados aún no está integrado.");
        }

        /// <summary>Autoriza el cobro con Banco y luego asigna la propiedad en el tablero.</summary>
        public ResultadoAccionJuego ComprarPropiedad(string idJugador)
        {
            JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
            Propiedad propiedad = jugador?.Posicion?.Casilla as Propiedad;

            if (jugador is null || propiedad is null)
                return new ResultadoAccionJuego(false, "El jugador no está sobre una propiedad.");

            if (!_tablero.PuedeComprarPropiedad(jugador, propiedad))
                return new ResultadoAccionJuego(false, "La propiedad ya tiene propietario.");

            ResultadoOperacion pago = _servidor.ProcesarCompraPropiedad(
                jugador.IdJugador,
                propiedad.Precio,
                propiedad.Nombre,
                NumeroTurnoActual);

            if (!pago.FueExitosa)
                return new ResultadoAccionJuego(false, pago.Mensaje);

            if (!_tablero.AsignarPropiedad(jugador, propiedad))
                return new ResultadoAccionJuego(false, "El pago fue autorizado, pero no se pudo asignar la propiedad.");

            _servidor.ActualizarCantidadPropiedadesDesdeTablero(
                jugador.IdJugador,
                _tablero.ContarPropiedadesDe(jugador));

            return new ResultadoAccionJuego(true, "Propiedad comprada.", propiedad.Nombre);
        }

        /// <summary>Registra que el jugador rechaza la compra de la propiedad actual.</summary>
        public ResultadoAccionJuego NoComprarPropiedad(string idJugador)
        {
            JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
            Propiedad propiedad = jugador?.Posicion?.Casilla as Propiedad;

            if (jugador is null || propiedad is null || !propiedad.Disponible)
                return new ResultadoAccionJuego(false, "No hay una propiedad disponible para rechazar.");

            return new ResultadoAccionJuego(true, "El jugador decidió no comprar la propiedad.", propiedad.Nombre);
        }

        /// <summary>Solicita a Banco el alquiler de la propiedad actual del jugador.</summary>
        public ResultadoAccionJuego PagarAlquilerActual(string idJugador)
        {
            JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
            Propiedad propiedad = jugador?.Posicion?.Casilla as Propiedad;

            if (jugador is null || propiedad is null)
                return new ResultadoAccionJuego(false, "El jugador no está sobre una propiedad.");

            if (!_tablero.PuedePagarAlquiler(jugador, propiedad))
                return new ResultadoAccionJuego(false, "La propiedad no requiere pago de alquiler.");

            ResultadoOperacion alquiler = _servidor.ProcesarPagoAlquiler(
                jugador.IdJugador,
                propiedad.Propietario.IdJugador,
                propiedad.Alquiler,
                propiedad.Nombre,
                NumeroTurnoActual);

            return alquiler.FueExitosa
                ? new ResultadoAccionJuego(true, "Alquiler pagado.", propiedad.Nombre)
                : new ResultadoAccionJuego(false, alquiler.Mensaje);
        }

        /// <summary>Libera propiedades y retira al jugador eliminado de la cola.</summary>
        public ResultadoAccionJuego EliminarJugadorDelJuego(string idJugador)
        {
            JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
            if (jugador is null)
                return new ResultadoAccionJuego(false, "No existe una representación del jugador en el tablero.");

            _tablero.EliminarJugador(jugador);
            bool eliminadoDeTurnos = _turnos.EliminarJugador(idJugador);
            return eliminadoDeTurnos
                ? new ResultadoAccionJuego(true, "Jugador retirado de tablero y turnos.")
                : new ResultadoAccionJuego(false, "El jugador fue retirado del tablero, pero no estaba en la cola de turnos.");
        }

        /// <summary>Avanza la cola solo si el jugador actual termina su turno.</summary>
        public ResultadoAccionJuego TerminarTurno(string idJugador)
        {
            JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
            if (jugador is null || !EsTurnoActual(idJugador))
                return new ResultadoAccionJuego(false, "Solo el jugador del turno actual puede terminarlo.");

            NodoJugador siguiente = _turnos.AvanzarTurno(jugador);
            if (siguiente is null)
                return new ResultadoAccionJuego(false, "No fue posible avanzar al siguiente turno.");

            NumeroTurnoActual++;
            return new ResultadoAccionJuego(true, "Turno terminado.", siguiente.IdJugador);
        }

        /// <summary>
        /// Aplica un movimiento entregado por el módulo de dados y solicita al
        /// servidor el premio de cada paso por inicio.
        /// </summary>
        public ResultadoAccionJuego MoverJugador(string idJugador, int cantidadCasillas)
        {
            JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
            if (jugador is null || !EsTurnoActual(idJugador))
                return new ResultadoAccionJuego(false, "El jugador no puede moverse fuera de su turno.");

            ResultadoMovimientoTablero movimiento = _tablero.MoverJugador(jugador.Posicion, cantidadCasillas, jugador);
            if (movimiento.PosicionFinal is null)
                return new ResultadoAccionJuego(false, "No fue posible mover al jugador.");

            ResultadoOperacion posicion = _servidor.ActualizarPosicionDesdeTablero(
                jugador.IdJugador,
                _tablero.ObtenerIndiceDeNodo(movimiento.PosicionFinal));
            if (!posicion.FueExitosa)
                return new ResultadoAccionJuego(false, posicion.Mensaje);

            for (int i = 0; i < movimiento.VecesPasoPorInicio; i++)
            {
                ResultadoOperacion premio = _servidor.ProcesarPremioInicio(
                    jugador.IdJugador,
                    _tablero.PremioInicio,
                    NumeroTurnoActual);

                if (!premio.FueExitosa)
                    return new ResultadoAccionJuego(false, premio.Mensaje);
            }

            return new ResultadoAccionJuego(
                true,
                "Jugador movido.",
                $"Posicion={_tablero.ObtenerIndiceDeNodo(movimiento.PosicionFinal)}");
        }

        /// <summary>Aplica la parte espacial de un evento y solicita al servidor su efecto económico.</summary>
        public ResultadoAccionJuego AplicarEvento(string idJugador, CartaEvento carta)
        {
            JugadorTablero jugador = ObtenerJugadorTablero(idJugador);
            if (jugador is null || !EsTurnoActual(idJugador))
                return new ResultadoAccionJuego(false, "El jugador no puede aplicar un evento fuera de su turno.");

            ResultadoEventoTablero evento = _tablero.EjecutarCartaEvento(carta, jugador);
            ResultadoOperacion dinero = evento.TipoEvento switch
            {
                "GanarDinero" => _servidor.ProcesarGananciaEvento(jugador.IdJugador, evento.Monto, carta.Descripcion, NumeroTurnoActual),
                "PerderDinero" => _servidor.ProcesarPerdidaEvento(jugador.IdJugador, evento.Monto, carta.Descripcion, NumeroTurnoActual),
                _ => ResultadoOperacion.Exito("Evento sin cambio monetario.", 0, 0)
            };

            if (!dinero.FueExitosa)
                return new ResultadoAccionJuego(false, dinero.Mensaje);

            if (evento.VecesPasoPorInicio > 0)
            {
                for (int i = 0; i < evento.VecesPasoPorInicio; i++)
                {
                    ResultadoOperacion premio = _servidor.ProcesarPremioInicio(
                        jugador.IdJugador,
                        _tablero.PremioInicio,
                        NumeroTurnoActual);
                    if (!premio.FueExitosa)
                        return new ResultadoAccionJuego(false, premio.Mensaje);
                }
            }

            _servidor.ActualizarPosicionDesdeTablero(jugador.IdJugador, _tablero.ObtenerIndiceDeNodo(jugador.Posicion));
            return new ResultadoAccionJuego(true, "Evento aplicado.", evento.TipoEvento);
        }

        /// <summary>Devuelve la representación espacial sin exponer el arreglo interno.</summary>
        public JugadorTablero ObtenerJugadorTablero(string idJugador)
        {
            if (string.IsNullOrWhiteSpace(idJugador))
                return null;

            for (int i = 0; i < _jugadores.Length; i++)
            {
                JugadorTablero jugador = _jugadores[i];
                if (jugador is not null && jugador.IdJugador.Equals(idJugador, StringComparison.OrdinalIgnoreCase))
                    return jugador;
            }

            return null;
        }
    }
}
