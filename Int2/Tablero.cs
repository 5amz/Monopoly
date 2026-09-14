namespace Monopoly
{
    public class Tablero
    {
        public NodoCasilla Head {get; set;}
        public NodoCasilla Tail {get; set;}
        public int Cantidad {get; set;}
        public decimal PremioInicio {get; set;}

        public Tablero()
        {
            Head = null;
            Tail = null;
            Cantidad = 0;
            PremioInicio = 200000;
        }

        public void AgregarCasilla(Casilla casilla)
        {
            NodoCasilla nuevoNodo = new NodoCasilla(casilla);

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
            {
                return null;
            }

            NodoCasilla actual = Head;
            for (int i = 0; i < posicion; i++)
            {
                actual = actual.Next;
            }
            return actual;
        }

        public Casilla ObtenerCasilla(int posicion)
        {
            return ObtenerNodo(posicion).Casilla;
        }

        public NodoCasilla MoverJugador(NodoCasilla posicionActual, int cantidadCasillas, JugadorTablero jugador)
        {
            if (posicionActual == null || jugador == null)
            {
                return null;
            }

            NodoCasilla posicion = posicionActual;
            if (cantidadCasillas > 0)
            {
                for (int i = 0; i < cantidadCasillas; i++)
                {
                    posicion = posicion.Next;

                    if (posicion == Head)
                    {
                        jugador.Dinero += PremioInicio;
                    }
                }
            }
            else if (cantidadCasillas < 0)
            {
                for (int i = 0; i < -cantidadCasillas; i++)
                {
                    posicion = posicion.Prev;
                }
            }
            
            return posicion;
        }

        public bool ComprarPropiedad(JugadorTablero jugador, Propiedad propiedad)
        {
            if (jugador == null || propiedad == null || !propiedad.Disponible)
            {
                return false;
            }

            if (jugador.Dinero >= propiedad.Precio)
            {
                jugador.Dinero -= propiedad.Precio;
                propiedad.Disponible = false;
                propiedad.Propietario = jugador;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool PagarAlquiler(JugadorTablero jugador, Propiedad propiedad)
        {
            if (jugador == null || propiedad == null || propiedad.Disponible || propiedad.Propietario == null || propiedad.Propietario == jugador)
            {
                return false;
            }

            if (jugador.Dinero >= propiedad.Alquiler)
            {
                jugador.Dinero -= propiedad.Alquiler;
                propiedad.Propietario.Dinero += propiedad.Alquiler;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void EjecutarCartaEvento(CartaEvento carta, JugadorTablero jugador)
        {
            if (carta == null || jugador == null)
            {
                return;
            }

            switch (carta.Tipo)
            {
                case "GanarDinero":
                    jugador.Dinero += carta.Valor;
                    break;

                case "PerderDinero":
                    jugador.Dinero -= carta.Valor;
                    break;

                case "Avanzar":
                    jugador.Posicion = MoverJugador(jugador.Posicion, carta.Valor, jugador);
                    break;

                case "Retroceder":
                    jugador.Posicion = MoverJugador(jugador.Posicion, carta.Valor, jugador);
                    break;

                case "PerderTurno":
                    jugador.PierdeTurno = true;
                    break;

                case "IrACasilla":
                    jugador.Posicion = ObtenerNodo(carta.Valor);
                    break;

                default:
                    return;
            }
        }

        public decimal CalcularPatrimonio(JugadorTablero jugador)
        {
            if (jugador == null)
            {
                return 0;
            }

            decimal patrimonio = jugador.Dinero;
            NodoCasilla actual = Head;

            for (int i = 0; i < Cantidad; i++)
            {
                if (actual.Casilla is Propiedad propiedad && propiedad.Propietario == jugador)
                {
                    patrimonio += propiedad.Precio;
                }
                actual = actual.Next;
            }
            
            return patrimonio;
        }

        public void EliminarJugador(JugadorTablero jugador)
        {
            if (jugador == null)
            {
                return;
            }

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
            {
                return 0;
            }

            int cantidadActivos = 0;

            for (int i = 0; i < jugadores.Length; i++)
            {
                if (jugadores[i] != null && jugadores[i].Activo)
                {
                    cantidadActivos++;
                }
            }

            return cantidadActivos;
        }

        public bool PartidaTerminada(JugadorTablero[] jugadores, int turnoActual, int maxTurnos)
        {
            if (jugadores == null || 0 >= maxTurnos)
            {
                return true;
            }

            int jugadoresActivos = ContarJugadoresActivos(jugadores);

            if (jugadoresActivos <= 1 || turnoActual >= maxTurnos)
            {
                return true;
            }

            return false;
        }

        public JugadorTablero ObtenerGanador(JugadorTablero[] jugadores)
        {
            if (jugadores == null)
            {
                return null;
            }

            JugadorTablero ganador = null;
            decimal maxPatrimonio = decimal.MinValue;

            for (int i = 0; i < jugadores.Length; i++)
            {
                if (jugadores[i] == null || !jugadores[i].Activo)
                {
                    continue;
                }

                decimal patrimonio = CalcularPatrimonio(jugadores[i]);

                if (ganador == null || patrimonio > maxPatrimonio)
                {
                    ganador = jugadores[i];
                    maxPatrimonio = patrimonio;
                }
            }
            
            return ganador;
        }
    }
}