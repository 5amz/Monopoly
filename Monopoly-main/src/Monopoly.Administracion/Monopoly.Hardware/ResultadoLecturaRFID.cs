#nullable enable

namespace Monopoly.Hardware;

public sealed class ResultadoLecturaRFID
{
    public bool FueEncontrado {get;}

    public string? IdJugador {get;}

// Crea el objeto.
    public ResultadoLecturaRFID(
        bool fueEncontrado,
        string? idJugador)
    {
        FueEncontrado = fueEncontrado;
        IdJugador = idJugador;
    }
}
