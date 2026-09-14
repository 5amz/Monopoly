namespace Monopoly
{
    public class ColaTurnos
    {
        public NodoJugador Head {get;  private set;}
        public NodoJugador Tail {get; private set;}
        public int Cantidad {get; private set;}

        public ColaTurnos()
        {
            Head = null;
            Tail = null;
            Cantidad = 0;
        }

        public void AgregarJugador(JugadorTablero jugador)
        {
            if (jugador == null)
            {
                return;
            }

            NodoJugador nuevoNodo = new NodoJugador(jugador);

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
            if (Head == null)
            {
                return null;
            }

            return Head;
        }

        public NodoJugador BuscarJugador(int jugadorId)
        {
            if (Head == null)
            {
                return null;
            }

            NodoJugador actual = Head;

            for (int i = 0; i < Cantidad; i++)
            {
                if (actual.JugadorId == jugadorId)
                {
                    return actual;
                }
                actual = actual.Next;
            }

            return null;
        }

        public NodoJugador AvanzarTurno(JugadorTablero jugador)
        {
            if (Head == null || jugador == null || Head.JugadorId != jugador.JugadorId)
            {
                return null;
            }

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
    }
}