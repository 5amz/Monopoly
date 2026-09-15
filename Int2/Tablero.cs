namespace Monopoly
{
    /// <summary>
    /// Lista circular doblemente enlazada de casillas. Administra posiciones y
    /// propiedades, pero delega todo dinero a ServidorJuego y Banco.
    /// </summary>
    public class Tablero
    {
        public NodoCasilla Head { get; set; }
        public NodoCasilla Tail { get; set; }
        public int Cantidad { get; set; }
        public decimal PremioInicio { get; set; }

        public Tablero()
        {
            Head = null;
            Tail = null;
            Cantidad = 0;
            PremioInicio = 200000;
        }

        public void AgregarCasilla(Casilla casilla)
        {
            var nuevoNodo = new NodoCasilla(casilla);
            if (Head == null)
            {
                Head = nuevoNodo;
                Tail = nuevoNodo;
                Head.Prev = Head;
                Head.Next = Head;
            }
            else
            {
                Tail.Next = nuevoNodo;
                nuevoNodo.Prev = Tail;
                nuevoNodo.Next = Head;
                Head.Prev = nuevoNodo;
                Tail = nuevoNodo;
            }

            Cantidad++;
        }

        public NodoCasilla ObtenerNodo(int posicion)
        {
            if (posicion < 0 || posicion >= Cantidad)
                return null;

            NodoCasilla actual = Head;
            for (int i = 0; i < posicion; i++)
                actual = actual.Next;

            return actual;
        }

        public Casilla ObtenerCasilla(int posicion)
        {
            return ObtenerNodo(posicion)?.Casilla;
        }

        /// <summary>Mueve al jugador y reporta cuántas veces pasó por inicio.</summary>
        public ResultadoMovimientoTablero MoverJugador(NodoCasilla posicionActual, int cantidadCasillas, JugadorTablero jugador)
        {
            if (posicionActual == null || jugador == null)
                return new ResultadoMovimientoTablero(null, 0);

            NodoCasilla posicion = posicionActual;
            int vecesPasoPorInicio = 0;

            if (cantidadCasillas >= 0)
            {
                for (int i = 0; i < cantidadCasillas; i++)
                {
                    posicion = posicion.Next;
                    if (posicion == Head)
                        vecesPasoPorInicio++;
                }
            }
            else
            {
                for (int i = 0; i < -cantidadCasillas; i++)
                    posicion = posicion.Prev;
            }

            jugador.Posicion = posicion;
            return new ResultadoMovimientoTablero(posicion, vecesPasoPorInicio);
        }

        /// <summary>Indica si una propiedad puede ser comprada estructuralmente.</summary>
        public bool PuedeComprarPropiedad(JugadorTablero jugador, Propiedad propiedad)
        {
            return jugador is not null && propiedad is not null && propiedad.Disponible;
        }

        /// <summary>Asigna la propiedad solo después de que Banco autorice el pago.</summary>
        public bool AsignarPropiedad(JugadorTablero jugador, Propiedad propiedad)
        {
            if (!PuedeComprarPropiedad(jugador, propiedad))
                return false;

            propiedad.Disponible = false;
            propiedad.Propietario = jugador;
            return true;
        }

        /// <summary>Indica si el jugador debe pagar alquiler y cuál es el propietario.</summary>
        public bool PuedePagarAlquiler(JugadorTablero jugador, Propiedad propiedad)
        {
            return jugador is not null
                && propiedad is not null
                && !propiedad.Disponible
                && propiedad.Propietario is not null
                && !propiedad.Propietario.IdJugador.Equals(jugador.IdJugador, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Aplica el efecto espacial de una carta y reporta su efecto económico.</summary>
        public ResultadoEventoTablero EjecutarCartaEvento(CartaEvento carta, JugadorTablero jugador)
        {
            if (carta == null || jugador == null)
                return new ResultadoEventoTablero("INVALIDO");

            switch (carta.Tipo)
            {
                case "GanarDinero":
                    return new ResultadoEventoTablero("GanarDinero", carta.Valor);

                case "PerderDinero":
                    return new ResultadoEventoTablero("PerderDinero", carta.Valor);

                case "Avanzar":
                {
                    ResultadoMovimientoTablero movimiento = MoverJugador(jugador.Posicion, carta.Valor, jugador);
                    return new ResultadoEventoTablero("Avanzar", 0, movimiento.VecesPasoPorInicio);
                }

                case "Retroceder":
                    MoverJugador(jugador.Posicion, -carta.Valor, jugador);
                    return new ResultadoEventoTablero("Retroceder");

                case "PerderTurno":
                    jugador.PierdeTurno = true;
                    return new ResultadoEventoTablero("PerderTurno");

                case "IrACasilla":
                    jugador.Posicion = ObtenerNodo(carta.Valor);
                    return new ResultadoEventoTablero("IrACasilla");

                default:
                    return new ResultadoEventoTablero("INVALIDO");
            }
        }

        /// <summary>Calcula el patrimonio usando el saldo oficial recibido del servidor.</summary>
        public decimal CalcularPatrimonio(JugadorTablero jugador, decimal saldoOficial)
        {
            if (jugador == null || saldoOficial < 0)
                return 0;

            decimal patrimonio = saldoOficial;
            NodoCasilla actual = Head;
            for (int i = 0; i < Cantidad; i++)
            {
                if (actual.Casilla is Propiedad propiedad && propiedad.Propietario == jugador)
                    patrimonio += propiedad.Precio;

                actual = actual.Next;
            }

            return patrimonio;
        }

        public void EliminarJugador(JugadorTablero jugador)
        {
            if (jugador == null)
                return;

            jugador.Activo = false;
            NodoCasilla actual = Head;
            for (int i = 0; i < Cantidad; i++)
            {
                if (actual.Casilla is Propiedad propiedad && propiedad.Propietario == jugador)
                {
                    propiedad.Disponible = true;
                    propiedad.Propietario = null;
                }

                actual = actual.Next;
            }
        }

        public int ContarJugadoresActivos(JugadorTablero[] jugadores)
        {
            if (jugadores == null)
                return 0;

            int cantidadActivos = 0;
            for (int i = 0; i < jugadores.Length; i++)
            {
                if (jugadores[i] is not null && jugadores[i].Activo)
                    cantidadActivos++;
            }

            return cantidadActivos;
        }

        public bool PartidaTerminada(JugadorTablero[] jugadores, int turnoActual, int maxTurnos)
        {
            if (jugadores == null || maxTurnos <= 0)
                return true;

            return ContarJugadoresActivos(jugadores) <= 1 || turnoActual >= maxTurnos;
        }

        public JugadorTablero ObtenerGanador(JugadorTablero[] jugadores, Func<string, decimal> consultarSaldoOficial)
        {
            if (jugadores == null || consultarSaldoOficial == null)
                return null;

            JugadorTablero ganador = null;
            decimal maxPatrimonio = decimal.MinValue;
            for (int i = 0; i < jugadores.Length; i++)
            {
                JugadorTablero jugador = jugadores[i];
                if (jugador is null || !jugador.Activo)
                    continue;

                decimal patrimonio = CalcularPatrimonio(jugador, consultarSaldoOficial(jugador.IdJugador));
                if (ganador is null || patrimonio > maxPatrimonio)
                {
                    ganador = jugador;
                    maxPatrimonio = patrimonio;
                }
            }

            return ganador;
        }
    }
}
