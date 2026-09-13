namespace Monopoly
{
    public class NodoJugador
    {
        public int JugadorId {get; set;}
        public string Nombre {get; set;}
        public NodoJugador Next {get; set;}

        public NodoJugador(int jugadorId, string nombre)
        {
            JugadorId = jugadorId;
            Nombre = nombre;
            Next = null;
        }
    }
}