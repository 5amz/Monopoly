namespace Monopoly
{
    public class Casilla
    {
        public int Id {get; set;}
        public string Nombre {get; set;}
// Crea el objeto.
        public Casilla(int id, string nombre)
        {
            Id = id;
            Nombre = nombre;
        }

// Ejecuta ObtenerInformacion.
        public virtual string ObtenerInformacion()
        {
            return Nombre;
        }

        
// Ejecuta Resolver.
        public virtual Administracion.ResultadoAccionJuego Resolver(CoordinadorPartidaTablero partida, JugadorTablero jugador)
            => new(true, $"La casilla {Nombre} no requiere una operación adicional.");
    }
}
