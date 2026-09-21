#nullable enable

namespace Monopoly.Hardware;

public sealed class RegistroTarjetasRFID
{
    private readonly Dictionary<string, string> _tarjetas =
        new()
        {
            {"623845514E", "J1"},
            {"A98F8656F6", "J2"},
            {"D03B903249", "J3"},
            {"2203D734C2", "J4"}
        };

    public ResultadoLecturaRFID BuscarJugador(
        string uid)
    {
        if (_tarjetas.TryGetValue(uid, out string? jugador))
        {
            return new ResultadoLecturaRFID(
                true,
                jugador);
        }

        return new ResultadoLecturaRFID(
            false,
            null);
    }
        
}
