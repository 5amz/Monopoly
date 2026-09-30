using System;
using System.Drawing;
using System.IO;
using Monopoly.Protocolo;

namespace Monopoly.Cliente;
internal static class RecursosVista
{
    private sealed record ImagenVista(string Nombre, Image Imagen);
    private static readonly ListaSimple<ImagenVista> imagenes = new();
    private static readonly ListaSimple<string> incidencias = new();
    public static string Carpeta { get; private set; } = "";
    public static string Informe => string.Join(Environment.NewLine, incidencias);

    
// Ejecuta Cargar.
    public static void Cargar(string? carpeta = null)
    {
        Liberar();
        Carpeta = carpeta ?? Path.Combine(AppContext.BaseDirectory, "Imagenes");
        CargarImagen("fondo_tablero");
        CargarImagen("logo_central");
        CargarImagen("mazo_eventos");
        for (int n = 1; n <= 24; n++) CargarImagen("casilla_" + n.ToString("D2"));
        foreach (string carta in new[] { "ganar", "perder", "avanzar", "retroceder", "turno", "viajar" })
            CargarImagen("carta_" + carta);
        for (int n = 1; n <= 4; n++)
        {
            CargarImagen("ficha_j" + n);
            CargarImagen("propiedad_j" + n);
        }
        for (int n = 1; n <= 6; n++) CargarImagen("dado_" + n);
    }

    
// Ejecuta CargarImagen.
    private static void CargarImagen(string nombre)
    {
        try
        {
            using var archivo = File.OpenRead(Path.Combine(Carpeta, nombre + ".png"));
            Span<byte> firma = stackalloc byte[12];
            int leidos = archivo.Read(firma);
            archivo.Position = 0;
            if (leidos == 12 && firma[..4].SequenceEqual("RIFF"u8) && firma[8..12].SequenceEqual("WEBP"u8))
            {
                incidencias.Agregar(nombre + ".png — su contenido es WebP. Exporta la imagen como PNG; cambiar solo la extensión no la convierte.");
                return;
            }
            using var original = Image.FromStream(archivo);
            imagenes.Agregar(new ImagenVista(nombre, new Bitmap(original)));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException or System.Runtime.InteropServices.ExternalException)
        {
            incidencias.Agregar(nombre + ".png — " + (error is FileNotFoundException or DirectoryNotFoundException
                ? "no está; se omite." : "no se pudo leer; se omite."));
        }
    }

    
// Ejecuta Obtener.
    public static Image? Obtener(string nombre)
    {
        foreach (var imagen in imagenes)
            if (imagen.Nombre == nombre) return imagen.Imagen;
        return null;
    }

    
// Ejecuta Liberar.
    public static void Liberar()
    {
        foreach (var imagen in imagenes) imagen.Imagen.Dispose();
        imagenes.Limpiar();
        incidencias.Limpiar();
    }
}
