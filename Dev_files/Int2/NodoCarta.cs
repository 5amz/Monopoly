namespace Monopoly
{
    public class NodoCarta
    {
        public CartaEvento Carta;
        public NodoCarta Next {get; set;}

        public NodoCarta(CartaEvento carta)
        {
            Carta = carta;
            Next = null;
        }
    }
}