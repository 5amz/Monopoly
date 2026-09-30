namespace Monopoly
{
    public class Casilla
    {
        public int Id {get; set;}
        public string Nombre {get; set;}
        public Casilla(int id, string nombre)
        {
            Id = id;
            Nombre = nombre;
        }

        public virtual string ObtenerInformacion()
        {
            return Nombre;
        }

        // Polimorfismo de comportamiento: cada casilla solicita su regla al coordinador.
        public virtual Administracion.ResultadoAccionJuego Resolver(CoordinadorPartidaTablero partida, JugadorTablero jugador)
            => new(true, $"La casilla {Nombre} no requiere una operación adicional.");
    }
}
