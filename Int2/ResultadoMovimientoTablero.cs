namespace Monopoly
{
    /// <summary>Resultado espacial de un movimiento; no modifica dinero.</summary>
    public sealed class ResultadoMovimientoTablero
    {
        public NodoCasilla PosicionFinal { get; }
        public int VecesPasoPorInicio { get; }

        public ResultadoMovimientoTablero(NodoCasilla posicionFinal, int vecesPasoPorInicio)
        {
            PosicionFinal = posicionFinal;
            VecesPasoPorInicio = vecesPasoPorInicio;
        }
    }
}
