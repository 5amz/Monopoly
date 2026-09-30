namespace Monopoly
{
    
    public class ColaTurnos
    {
        public NodoJugador Head { get; private set; }
        public NodoJugador Tail { get; private set; }
        public int Cantidad { get; private set; }

// Crea el objeto.
        public ColaTurnos()
        {
            Head = null;
            Tail = null;
            Cantidad = 0;
        }

// Ejecuta AgregarJugador.
        public void AgregarJugador(JugadorTablero jugador)
        {
            if (jugador == null || !jugador.Activo || BuscarJugador(jugador.IdJugador) is not null)
                return;

            var nuevoNodo = new NodoJugador(jugador);
            if (Head == null)
            {
                Head = nuevoNodo;
                Tail = nuevoNodo;
                Head.Next = Head;
            }
            else
            {
                nuevoNodo.Next = Head;
                Tail.Next = nuevoNodo;
                Tail = nuevoNodo;
            }

            Cantidad++;
        }

// Ejecuta ObtenerJugadorActual.
        public NodoJugador ObtenerJugadorActual()
        {
            return Head;
        }

// Ejecuta BuscarJugador.
        public NodoJugador BuscarJugador(string idJugador)
        {
            if (Head == null || string.IsNullOrWhiteSpace(idJugador))
                return null;

            NodoJugador actual = Head;
            for (int i = 0; i < Cantidad; i++)
            {
                if (actual.IdJugador.Equals(idJugador, StringComparison.OrdinalIgnoreCase))
                    return actual;

                actual = actual.Next;
            }

            return null;
        }

// Ejecuta AvanzarTurno.
        public NodoJugador AvanzarTurno(JugadorTablero jugador)
        {
            if (Head == null || jugador == null || !Head.IdJugador.Equals(jugador.IdJugador, StringComparison.OrdinalIgnoreCase))
                return null;

            Head = Head.Next;
            Tail = Tail.Next;

            OmitirTurnosPerdidos();

            return Head;
        }

        
// Ejecuta OmitirTurnosPerdidos.
        public void OmitirTurnosPerdidos()
        {
            while (Head is not null && Head.Jugador.PierdeTurno)
            {
                Head.Jugador.PierdeTurno = false;
                Head = Head.Next;
                Tail = Tail.Next;
            }
        }

        
// Ejecuta EliminarJugador.
        public bool EliminarJugador(string idJugador)
        {
            if (Head is null || string.IsNullOrWhiteSpace(idJugador))
                return false;

            NodoJugador anterior = Tail;
            NodoJugador actual = Head;
            for (int i = 0; i < Cantidad; i++)
            {
                if (!actual.IdJugador.Equals(idJugador, StringComparison.OrdinalIgnoreCase))
                {
                    anterior = actual;
                    actual = actual.Next;
                    continue;
                }

                if (Cantidad == 1)
                {
                    Head = null;
                    Tail = null;
                }
                else
                {
                    anterior.Next = actual.Next;
                    if (ReferenceEquals(actual, Head))
                        Head = actual.Next;

                    if (ReferenceEquals(actual, Tail))
                        Tail = anterior;
                }

                Cantidad--;
                return true;
            }

            return false;
        }
    }
}
