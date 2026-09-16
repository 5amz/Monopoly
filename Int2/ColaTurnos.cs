namespace Monopoly
{
    /// <summary>Cola circular propia para controlar el orden de los turnos.</summary>
    public class ColaTurnos
    {
        public NodoJugador Head { get; private set; }
        public NodoJugador Tail { get; private set; }
        public int Cantidad { get; private set; }

        public ColaTurnos()
        {
            Head = null;
            Tail = null;
            Cantidad = 0;
        }

        public void AgregarJugador(JugadorTablero jugador)
        {
            if (jugador == null)
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

        public NodoJugador ObtenerJugadorActual()
        {
            return Head;
        }

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

        public NodoJugador AvanzarTurno(JugadorTablero jugador)
        {
            if (Head == null || jugador == null || !Head.IdJugador.Equals(jugador.IdJugador, StringComparison.OrdinalIgnoreCase))
                return null;

            Head = Head.Next;
            Tail = Tail.Next;

            while (Head.Jugador.PierdeTurno)
            {
                Head.Jugador.PierdeTurno = false;
                Head = Head.Next;
                Tail = Tail.Next;
            }

            return Head;
        }

        /// <summary>Retira un jugador de la cola circular al quedar eliminado.</summary>
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
