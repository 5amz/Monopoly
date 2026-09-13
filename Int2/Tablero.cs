namespace Monopoly
{
    public class Tablero
    {
        public NodoCasilla Head {get; set;}
        public NodoCasilla Tail {get; set;}
        public int Cantidad {get; set;}

        public Tablero()
        {
            Head = null;
            Tail = null;
            Cantidad = 0;
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

        public NodoCasilla MoverJugador(NodoCasilla posicionActual, int cantidadCasillas)
        {
            if (posicionActual == null || cantidadCasillas < 0)
            {
                return null;
            }

            NodoCasilla posicion = posicionActual;
            for (int i = 0; i < cantidadCasillas; i++)
            {
                posicion = posicion.Next;
            }
            return posicion;
        }
    }
}