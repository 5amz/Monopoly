namespace Monopoly.Administracion;

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

    private static string Limpiar(string valor)
        => (valor ?? string.Empty).Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
}
