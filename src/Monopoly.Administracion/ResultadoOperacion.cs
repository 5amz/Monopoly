namespace Monopoly.Administracion;

//Respuesta sencilla para que el servidor pueda comunicar el resultado de una operación sin entregar acceso directo al Jugador.
public sealed class ResultadoOperacion
{
    public bool FueExitosa { get; }
    public string Mensaje { get; }
    public decimal SaldoAnterior { get; }
    public decimal SaldoActual { get; }
    public bool FueRechazadaPorFondosInsuficientes { get; }

    // Construye la respuesta con el resultado y los saldos involucrados
    private ResultadoOperacion(
        bool fueExitosa,
        string mensaje,
        decimal saldoAnterior,
        decimal saldoActual,
        bool fueRechazadaPorFondosInsuficientes)
    {
        FueExitosa = fueExitosa;
        Mensaje = mensaje;
        SaldoAnterior = saldoAnterior;
        SaldoActual = saldoActual;
        FueRechazadaPorFondosInsuficientes = fueRechazadaPorFondosInsuficientes;
    }

    // Crea una respuesta exitosa 
    public static ResultadoOperacion Exito(string mensaje, decimal saldoAnterior, decimal saldoActual)
        => new(true, mensaje, saldoAnterior, saldoActual, false);

    // Crea una respuesta de error sin cambiar el saldo
    public static ResultadoOperacion Error(
        string mensaje,
        decimal saldoActual = 0,
        bool fueRechazadaPorFondosInsuficientes = false)
        => new(false, mensaje, saldoActual, saldoActual, fueRechazadaPorFondosInsuficientes);
}
