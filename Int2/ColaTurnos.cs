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

        public void AgregarJugador(int jugadorId, string nombre)
        {
            NodoJugador nuevoNodo = new NodoJugador(jugadorId, nombre);

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

        public NodoJugador AvanzarTurno()
        {
            if (Head == null)
            {
                return null;
            }

            Head = Head.Next;
            Tail = Tail.Next;

            return Head;
        }
    }
}