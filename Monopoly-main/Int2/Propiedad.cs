namespace Monopoly
{
    public class Propiedad : Casilla
    {
        public decimal Precio {get; set;}
        public decimal Alquiler {get; set;}
        // La disponibilidad se deriva del propietario; nunca se guarda por separado.
        public bool Disponible => Propietario is null;
        public JugadorTablero Propietario {get; set;}

        public Propiedad(int id, string nombre, decimal precio, decimal alquiler) : base(id, nombre)
        {
            Precio = precio;
            Alquiler = alquiler;
            Propietario = null;
        }

        public override string ObtenerInformacion()
        {
            return $"{Nombre} - Precio: {Precio} - Alquiler: {Alquiler}";
        }

        public override Administracion.ResultadoAccionJuego Resolver(CoordinadorPartidaTablero partida, JugadorTablero jugador)
            => partida.ResolverPropiedad(jugador, this);
    }
}
