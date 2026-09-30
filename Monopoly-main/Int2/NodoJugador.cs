namespace Monopoly
{
    
    public class NodoJugador
    {
        public string IdJugador { get; }
        public string Nombre { get; }
        public NodoJugador Next { get; set; }
        public JugadorTablero Jugador { get; }

// Crea el objeto.
        public NodoJugador(JugadorTablero jugador)
        {
            if (jugador == null)
                throw new ArgumentNullException(nameof(jugador));

            IdJugador = jugador.IdJugador;
            Nombre = jugador.Nombre;
            Next = null;
            Jugador = jugador;
        }
    }
}
