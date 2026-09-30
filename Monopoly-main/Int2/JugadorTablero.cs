namespace Monopoly
{
    



    public class JugadorTablero
    {
        public string IdJugador { get; }
        public string Nombre { get; }
        public NodoCasilla Posicion { get; set; }
        public bool PierdeTurno { get; set; }
        public bool Activo { get; set; }

// Crea el objeto.
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

    }
}
