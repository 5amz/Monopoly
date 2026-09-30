namespace Monopoly
{
    public class NodoCasilla
    {
        public Casilla Casilla;
        public NodoCasilla Prev {get; set;}
        public NodoCasilla Next {get; set;}

        public NodoCasilla(Casilla casilla)
        {
            Casilla = casilla;
            Prev = null;
            Next = null;
        }
    }
}