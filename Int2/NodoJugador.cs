namespace Monopoly
{
    public class NodoJugador
    {
        public int JugadorId {get; set;}
        public string Nombre {get; set;}
        public NodoJugador Next {get; set;}
        public JugadorTablero Jugador {get; set;}

        public NodoJugador(JugadorTablero jugador)
        {
            if (jugador == null)
            {
                return;
            }
            
            JugadorId = jugador.JugadorId;
            Nombre = jugador.Nombre;
            Next = null;
            Jugador = jugador;
        }
    }
}