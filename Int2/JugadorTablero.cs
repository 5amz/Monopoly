namespace Monopoly
{
    /// <summary>
    /// Representa únicamente el estado espacial del jugador en el tablero.
    /// El saldo oficial pertenece a Banco y no se almacena aquí.
    /// </summary>
    public class JugadorTablero
    {
        public string IdJugador { get; }
        public string Nombre { get; }
        public NodoCasilla Posicion { get; set; }
        public bool PierdeTurno { get; set; }
        public bool Activo { get; set; }

        public JugadorTablero(string idJugador, string nombre, NodoCasilla posicion)
        {
            if (string.IsNullOrWhiteSpace(idJugador))
                throw new ArgumentException("El identificador es obligatorio.", nameof(idJugador));

            IdJugador = idJugador.Trim();
            Nombre = nombre ?? string.Empty;
            Posicion = posicion;
            PierdeTurno = false;
            Activo = true;
        }

        // Mantiene compatibilidad con pruebas que usan identificadores numéricos.
        public JugadorTablero(int jugadorId, string nombre, NodoCasilla posicion)
            : this(jugadorId.ToString(), nombre, posicion)
        {
        }
    }
}
