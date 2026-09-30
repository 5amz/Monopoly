namespace Monopoly.Administracion;


public sealed class Dado
{
    public int Valor { get; }

// Crea el objeto.
    public Dado(int valor)
    {
        if (valor is < 1 or > 6)
            throw new ArgumentOutOfRangeException(nameof(valor), "Una cara debe estar entre 1 y 6.");
        Valor = valor;
    }
}
