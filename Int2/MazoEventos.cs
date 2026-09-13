namespace Monopoly
{
    public class MazoEventos
    {
        public NodoCarta Head {get; private set;}
        public NodoCarta Tail {get; private set;}
        public int Cantidad {get; private set;}

        public MazoEventos()
        {
            Head = null;
            Tail = null;
            Cantidad = 0;
        }

        public void AgregarCarta(CartaEvento carta)
        {
            NodoCarta nuevoNodo = new NodoCarta(carta);

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

        public CartaEvento ObtenerSiguienteCarta()
        {
            if (Head == null)
            {
                return null;
            }

            CartaEvento carta = Head.Carta;

            if (Cantidad == 1)
            {
                return carta;
            }

            Head = Head.Next;
            Tail = Tail.Next;

            return carta;
        }
    }
}