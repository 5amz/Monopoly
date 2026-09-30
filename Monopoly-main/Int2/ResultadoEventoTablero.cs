namespace Monopoly
{
    
    public sealed class ResultadoEventoTablero
    {
        public string TipoEvento { get; }
        public decimal Monto { get; }
        public int VecesPasoPorInicio { get; }

// Crea el objeto.
        public ResultadoEventoTablero(string tipoEvento, decimal monto = 0, int vecesPasoPorInicio = 0)
        {
            TipoEvento = tipoEvento;
            Monto = monto;
            VecesPasoPorInicio = vecesPasoPorInicio;
        }
    }
}
