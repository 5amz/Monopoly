namespace Monopoly
{
    public class NodoCarta
    {
        public CartaEvento Carta;
        public NodoCarta Next {get; set;}

// Crea el objeto.
        public NodoCarta(CartaEvento carta)
        {
            Carta = carta;
            Next = null;
        }
    }
}
