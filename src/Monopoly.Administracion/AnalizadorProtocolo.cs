using System.Globalization;

namespace Monopoly.Administracion;

/// <summary>Valida el formato de las líneas enviadas por un cliente.</summary>
public static class AnalizadorProtocolo
{
    /// <summary>
    /// Analiza una línea del protocolo. Las partes se separan con |.
    /// CONECTAR requiere ID, nombre y saldo inicial; los demás comandos no reciben parámetros.
    /// </summary>
    public static bool IntentarAnalizar(string texto, out SolicitudProtocolo solicitud, out RespuestaProtocolo error)
    {
        solicitud = null;
        error = null;

        if (string.IsNullOrWhiteSpace(texto))
        {
            error = RespuestaProtocolo.Error("FORMATO_INVALIDO", "La solicitud no puede estar vacía.");
            return false;
        }

        string[] partes = texto.Split('|');
        string nombreComando = partes[0].Trim();
        if (!Enum.TryParse(nombreComando, true, out ComandoProtocolo comando))
        {
            error = RespuestaProtocolo.Error("COMANDO_DESCONOCIDO", "El comando enviado no está respaldado por el protocolo.");
            return false;
        }

        if (comando == ComandoProtocolo.CONECTAR)
        {
            if (partes.Length != 4 || string.IsNullOrWhiteSpace(partes[1]) || string.IsNullOrWhiteSpace(partes[2]))
            {
                error = RespuestaProtocolo.Error("FORMATO_INVALIDO", "Use CONECTAR|id|nombre|saldoInicial.");
                return false;
            }

            if (!decimal.TryParse(partes[3], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal saldoInicial) || saldoInicial < 0)
            {
                error = RespuestaProtocolo.Error("FORMATO_INVALIDO", "El saldo inicial debe ser un número no negativo, por ejemplo 1500.00.");
                return false;
            }

            solicitud = SolicitudProtocolo.Conectar(partes[1].Trim(), partes[2].Trim(), saldoInicial);
            return true;
        }

        if (partes.Length != 1)
        {
            error = RespuestaProtocolo.Error("FORMATO_INVALIDO", $"El comando {comando} no recibe parámetros.");
            return false;
        }

        solicitud = SolicitudProtocolo.CrearAccion(comando);
        return true;
    }
}
