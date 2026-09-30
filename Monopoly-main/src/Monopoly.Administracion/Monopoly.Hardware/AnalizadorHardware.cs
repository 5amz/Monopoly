#nullable enable
using System.Globalization;
using Monopoly.Administracion;

namespace Monopoly.Hardware;

/// <summary>Único analizador del límite serial, compartido por hardware y pruebas.</summary>
public static class AnalizadorHardware
{
    public static bool IntentarAnalizar(string? linea, out EventoHardware? evento)
    {
        evento = null;
        if (string.IsNullOrWhiteSpace(linea) || linea.Length > 80)
            return false;

        string[] partes = linea.Trim().Split('|');
        if (partes.Length == 2 && partes[0].Equals("RFID", StringComparison.OrdinalIgnoreCase))
        {
            if (!RegistroTarjetasRFID.IntentarNormalizarUid(partes[1], out string uid))
                return false;
            evento = new EventoHardware(TipoEventoHardware.RFID, uid);
            return true;
        }

        if (!partes[0].Equals("DADO", StringComparison.OrdinalIgnoreCase))
            return false;

        ResultadoDados resultado;
        if (partes.Length == 2 && LeerEntero(partes[1], out int totalAntiguo)
            && totalAntiguo is >= 2 and <= 12)
        {
            resultado = new ResultadoDados(totalAntiguo);
        }
        else if (partes.Length == 4 && LeerEntero(partes[1], out int dado1)
            && LeerEntero(partes[2], out int dado2) && LeerEntero(partes[3], out int total)
            && dado1 is >= 1 and <= 6 && dado2 is >= 1 and <= 6 && total == dado1 + dado2)
        {
            resultado = new ResultadoDados(dado1, dado2, total);
        }
        else
        {
            return false;
        }

        evento = new EventoHardware(TipoEventoHardware.Dado,
            resultado.Total.ToString(CultureInfo.InvariantCulture), resultado);
        return true;
    }

    private static bool LeerEntero(string texto, out int valor) =>
        int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out valor);
}
