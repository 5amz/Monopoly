namespace Monopoly
{
    
    public sealed class ResultadoMovimientoTablero
    {
        public NodoCasilla PosicionFinal { get; }
        public int VecesPasoPorInicio { get; }

// Crea el objeto.
        public ResultadoMovimientoTablero(NodoCasilla posicionFinal, int vecesPasoPorInicio)
        {
            PosicionFinal = posicionFinal;
            VecesPasoPorInicio = vecesPasoPorInicio;
        }
    }
}
