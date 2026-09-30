namespace Monopoly
{
    /// <summary>Nodo de la cola circular de turnos.</summary>
    public class NodoJugador
    {
        public string IdJugador { get; }
        public string Nombre { get; }
        public NodoJugador Next { get; set; }
        public JugadorTablero Jugador { get; }

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
