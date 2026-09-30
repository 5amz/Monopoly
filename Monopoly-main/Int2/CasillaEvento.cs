namespace Monopoly
{
    public class CasillaEvento : Casilla
    {
        public string DescripcionEvento {get; set;}

        public CasillaEvento(int id, string nombre, string descripcionEvento) : base(id, nombre)
        {
            DescripcionEvento = descripcionEvento;
        }

        public override string ObtenerInformacion()
        {
            return $"{Nombre} - Evento: {DescripcionEvento}";
        }

        public override Administracion.ResultadoAccionJuego Resolver(CoordinadorPartidaTablero partida, JugadorTablero jugador)
            => partida.ResolverEvento(jugador);
    }
}
