using System;
using System.IO;
using System.Text.Json;

namespace Monopoly.Nucleo;

public sealed class FragmentoMusical
{
    public string Archivo { get; set; } = "";
    public double InicioSegundos { get; set; }
    public double DuracionSegundos { get; set; } = .8;
    internal bool Omitida { get; set; }
}



public sealed class ConfiguracionMusica : IDisposable
{
    private JsonDocument? documento;
    public bool FondoActivado { get; private set; }
    public double SegundosPorCasilla { get; private set; } = .8;

// Ejecuta Cargar.
    public static ConfiguracionMusica Cargar(string carpeta, Action<string> informar)
    {
        var resultado = new ConfiguracionMusica();
        string archivo = Path.Combine(carpeta, "musica.json");
        if (!File.Exists(archivo)) return resultado;
        try
        {
            resultado.documento = JsonDocument.Parse(File.ReadAllText(archivo), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            var raiz = resultado.documento.RootElement;
            if (raiz.ValueKind == JsonValueKind.Null) return resultado;
            if (raiz.ValueKind != JsonValueKind.Object) throw new JsonException("Se esperaba un objeto de configuración.");
            if (Campo(raiz, "fondoActivado", out var fondo)) resultado.FondoActivado = fondo.GetBoolean();
            if (Campo(raiz, "segundosPorCasilla", out var segundos)) resultado.SegundosPorCasilla = segundos.GetDouble();
            if (Campo(raiz, "pistas", out var pistas) && pistas.ValueKind != JsonValueKind.Null)
                foreach (var pista in pistas.EnumerateObject()) Leer(pista.Value); 
            if (!double.IsFinite(resultado.SegundosPorCasilla) || resultado.SegundosPorCasilla < .15 || resultado.SegundosPorCasilla > 5)
            {
                informar("musica.json: segundosPorCasilla solo controla la ficha y debe estar entre 0.15 y 5; se usará 0.8. Se conservan los inicios y duraciones de las canciones.");
                resultado.SegundosPorCasilla = .8;
            }
            return resultado;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
        {
            resultado.Dispose(); informar("No se pudo leer musica.json: " + e.Message);
            return new ConfiguracionMusica();
        }
    }
// Ejecuta Campo.
    private static bool Campo(JsonElement objeto, string nombre, out JsonElement valor)
    {
        valor = default; bool existe = false;
        if (objeto.ValueKind != JsonValueKind.Object) return false;
        foreach (var campo in objeto.EnumerateObject())
            if (campo.Name.Equals(nombre, StringComparison.OrdinalIgnoreCase)) { valor = campo.Value; existe = true; }
        return existe;
    }
// Ejecuta Leer.
    private static FragmentoMusical? Leer(JsonElement valor)
    {
        if (valor.ValueKind == JsonValueKind.Null) return null;
        if (valor.ValueKind != JsonValueKind.Object) throw new JsonException("La pista debe ser un objeto.");
        var fragmento = new FragmentoMusical();
        if (Campo(valor, "archivo", out var archivo)) fragmento.Archivo = archivo.GetString() ?? "";
        if (Campo(valor, "inicioSegundos", out var inicio)) fragmento.InicioSegundos = inicio.GetDouble();
        if (Campo(valor, "duracionSegundos", out var duracion)) fragmento.DuracionSegundos = duracion.GetDouble();
        return fragmento;
    }
// Ejecuta Obtener.
    internal FragmentoMusical? Obtener(string clave, double duracion)
    {
        if (documento is not null && Campo(documento.RootElement, "pistas", out var pistas) && Campo(pistas, clave, out var pista))
            return Leer(pista);
        return new FragmentoMusical { Archivo = clave + ".mp3", DuracionSegundos = duracion };
    }
// Ejecuta Aplicar.
    public void Aplicar(CasillaVista casilla)
    {
        if (ReferenceEquals(casilla.ConfiguracionMusical, this)) return;
        casilla.Musica = casilla.Tipo == "EVENTO" ? null : Obtener(casilla.Imagen, .8);
        casilla.ConfiguracionMusical = this;
    }
// Ejecuta Aplicar.
    public void Aplicar(CartaVisible carta)
    {
        if (ReferenceEquals(carta.ConfiguracionMusical, this)) return;
        carta.Musica = carta.Imagen.Length == 0 ? null : Obtener(carta.Imagen, 5);
        carta.ConfiguracionMusical = this;
    }
// Ejecuta Dispose.
    public void Dispose() { documento?.Dispose(); documento = null; }
}
