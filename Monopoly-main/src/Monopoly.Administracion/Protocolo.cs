namespace Monopoly.Administracion;

/// <summary>Comandos que el protocolo del proyecto reconoce.</summary>
public enum ComandoProtocolo
{
    CONECTAR,
    TIRAR_DADOS,
    COMPRAR_PROPIEDAD,
    NO_COMPRAR,
    TERMINAR_TURNO,
    CONSULTAR_ESTADO,
    CONSULTAR_TRANSACCIONES
}

/// <summary>Representa una línea ya validada en su formato básico.</summary>
public sealed class SolicitudProtocolo
{
    public ComandoProtocolo Comando { get; }
    public string IdJugador { get; }
    public string NombreJugador { get; }

    private SolicitudProtocolo(ComandoProtocolo comando, string idJugador, string nombreJugador)
    {
        Comando = comando;
        IdJugador = idJugador;
        NombreJugador = nombreJugador;
    }

    /// <summary>Crea la solicitud de identificación sin aceptar datos monetarios.</summary>
    public static SolicitudProtocolo Conectar(string idJugador, string nombreJugador)
        => new(ComandoProtocolo.CONECTAR, idJugador, nombreJugador);

    /// <summary>Crea una solicitud que no necesita parámetros adicionales.</summary>
    public static SolicitudProtocolo CrearAccion(ComandoProtocolo comando)
        => new(comando, string.Empty, string.Empty);
}

/// <summary>Respuesta serializable que el servidor devuelve por TCP.</summary>
public sealed class RespuestaProtocolo
{
    public bool FueExitosa { get; }
    public string Codigo { get; }
    public string Mensaje { get; }
    public string Datos { get; }

    private RespuestaProtocolo(bool fueExitosa, string codigo, string mensaje, string datos)
    {
        FueExitosa = fueExitosa;
        Codigo = codigo;
        Mensaje = mensaje;
        Datos = datos;
    }

    /// <summary>Crea una respuesta de aceptación para un comando.</summary>
    public static RespuestaProtocolo Exito(ComandoProtocolo comando, string mensaje, string datos = "")
        => new(true, comando.ToString(), mensaje, datos);

    /// <summary>Crea una respuesta de rechazo con un código que el cliente puede mostrar.</summary>
    public static RespuestaProtocolo Error(string codigo, string mensaje)
        => new(false, codigo, mensaje, string.Empty);

    /// <summary>Convierte la respuesta en una línea de texto fácil de depurar.</summary>
    public string ConvertirALinea()
    {
        string tipo = FueExitosa ? "OK" : "ERROR";
        return string.IsNullOrEmpty(Datos)
            ? $"{tipo}|{Limpiar(Codigo)}|{Limpiar(Mensaje)}"
            : $"{tipo}|{Limpiar(Codigo)}|{Limpiar(Mensaje)}|{Limpiar(Datos)}";
    }

    /// <summary>Crea una notificación espontánea del servidor para clientes conectados.</summary>
    public static string CrearEvento(string codigo, string datos)
    {
        return $"EVENTO|{Limpiar(codigo)}|{Limpiar(datos)}";
    }

    private static string Limpiar(string valor)
        => (valor ?? string.Empty).Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
}

/// <summary>Valida el formato de las líneas enviadas por un cliente.</summary>
public static class AnalizadorProtocolo
{
    /// <summary>
    /// Analiza una línea del protocolo. Las partes se separan con |.
    /// CONECTAR requiere ID y nombre; el saldo inicial lo decide el servidor.
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
            if (partes.Length != 3 || string.IsNullOrWhiteSpace(partes[1]) || string.IsNullOrWhiteSpace(partes[2]))
            {
                error = RespuestaProtocolo.Error("FORMATO_INVALIDO", "Use CONECTAR|id|nombre.");
                return false;
            }

            solicitud = SolicitudProtocolo.Conectar(partes[1].Trim(), partes[2].Trim());
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
