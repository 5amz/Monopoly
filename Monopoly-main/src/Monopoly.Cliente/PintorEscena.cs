using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Monopoly.Nucleo;

namespace Monopoly.Cliente;
internal static class PintorEscena
{
    // Dibuja las imágenes y los textos del tablero.
    internal static void Pintar(Graphics dibujo, EscenaTablero escena)
    {
        dibujo.InterpolationMode = InterpolationMode.HighQualityBicubic;
        dibujo.PixelOffsetMode = PixelOffsetMode.HighQuality;
        foreach (var elemento in escena.Elementos)
        {
            var a = elemento.Area;
            var area = new RectangleF((float)a.X, (float)a.Y, (float)a.Ancho, (float)a.Alto);
            if (elemento.Tipo == "IMAGEN")
            {
                Image? imagen = RecursosVista.Obtener(elemento.Contenido);
                if (imagen is not null) dibujo.DrawImage(imagen, area);
            }
            else if (elemento.Tipo is "TIPO" or "PRECIO" or "TEXTO")
            {
                using var fondo = new SolidBrush(Color.FromArgb(228, 20, 20, 23));
                using var tinta = new SolidBrush(elemento.Tipo == "TIPO" ? TemaCiudad.Destacado : Color.White);
                if (elemento.Tipo == "TIPO") dibujo.FillEllipse(fondo, area);
                else dibujo.FillRectangle(fondo, area);
                float tamano = elemento.Tipo == "TIPO" ? Math.Clamp(area.Height * .65f, 10, 20) : Math.Clamp(area.Width / 12.5f, 9, 14);
                using var fuente = new Font("Segoe UI", tamano, FontStyle.Bold, GraphicsUnit.Pixel);
                using var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                dibujo.DrawString(elemento.Contenido, fuente, tinta, area, formato);
            }
        }
    }
}
