namespace Monopoly
{
    public class CasillaEspecial : Casilla
    {
        public string Tipo {get; set;}
        public decimal Monto { get; set; }

        public CasillaEspecial(int id, string nombre, string tipo, decimal monto = 0) : base(id, nombre)
        {
            Tipo = tipo;
            Monto = monto;
        }

        public override string ObtenerInformacion()
        {
            return $"{Nombre} - Tipo: {Tipo}";
        }

        public override Administracion.ResultadoAccionJuego Resolver(CoordinadorPartidaTablero partida, JugadorTablero jugador)
            => Tipo == "Pago" ? partida.ResolverImpuesto(jugador, this) : base.Resolver(partida, jugador);
    }
}
