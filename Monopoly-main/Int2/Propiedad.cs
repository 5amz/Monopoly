namespace Monopoly
{
    public class Propiedad : Casilla
    {
        public decimal Precio {get; set;}
        public decimal Alquiler {get; set;}
        
        public bool Disponible => Propietario is null;
        public JugadorTablero Propietario {get; set;}

// Crea el objeto.
        public Propiedad(int id, string nombre, decimal precio, decimal alquiler) : base(id, nombre)
        {
            Precio = precio;
            Alquiler = alquiler;
            Propietario = null;
        }

// Ejecuta ObtenerInformacion.
        public override string ObtenerInformacion()
        {
            return $"{Nombre} - Precio: {Precio} - Alquiler: {Alquiler}";
        }

// Ejecuta Resolver.
        public override Administracion.ResultadoAccionJuego Resolver(CoordinadorPartidaTablero partida, JugadorTablero jugador)
            => partida.ResolverPropiedad(jugador, this);
    }
}
