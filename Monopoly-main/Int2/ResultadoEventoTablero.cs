namespace Monopoly
{
    /// <summary>Describe el efecto de una carta sin alterar el saldo oficial.</summary>
    public sealed class ResultadoEventoTablero
    {
        public string TipoEvento { get; }
        public decimal Monto { get; }
        public int VecesPasoPorInicio { get; }

        public ResultadoEventoTablero(string tipoEvento, decimal monto = 0, int vecesPasoPorInicio = 0)
        {
            TipoEvento = tipoEvento;
            Monto = monto;
            VecesPasoPorInicio = vecesPasoPorInicio;
        }
    }
}
