namespace Monopoly
{
    public class JugadorTablero
    {
        public int JugadorId {get; set;}
        public string Nombre {get; set;}
        public NodoCasilla Posicion {get; set;}
        public decimal Dinero {get; set;}
        public bool PierdeTurno {get; set;}
        public bool Activo {get; set;}

        public JugadorTablero(int jugadorId, string nombre, NodoCasilla posicion)
        {
            JugadorId = jugadorId;
            Nombre = nombre;
            Posicion = posicion;
            Dinero = 0;
            PierdeTurno = false;
            Activo = true;
        }
    }
}