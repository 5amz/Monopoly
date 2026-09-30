namespace Monopoly
{
    public class CasillaEvento : Casilla
    {
        public string DescripcionEvento {get; set;}

// Crea el objeto.
        public CasillaEvento(int id, string nombre, string descripcionEvento) : base(id, nombre)
        {
            DescripcionEvento = descripcionEvento;
        }

// Ejecuta ObtenerInformacion.
        public override string ObtenerInformacion()
        {
            return $"{Nombre} - Evento: {DescripcionEvento}";
        }

// Ejecuta Resolver.
        public override Administracion.ResultadoAccionJuego Resolver(CoordinadorPartidaTablero partida, JugadorTablero jugador)
            => partida.ResolverEvento(jugador);
    }
}
