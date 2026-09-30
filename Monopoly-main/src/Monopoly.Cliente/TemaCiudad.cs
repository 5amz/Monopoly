using System.Drawing;
using System.Windows.Forms;

namespace Monopoly.Cliente;
internal static class TemaCiudad
{
    internal static readonly Color Fondo = Color.FromArgb(15, 16, 20);
    internal static readonly Color Panel = Color.FromArgb(28, 29, 35);
    internal static readonly Color Texto = Color.FromArgb(247, 242, 232);
    internal static readonly Color Acento = Color.FromArgb(213, 48, 62);
    internal static readonly Color Destacado = Color.FromArgb(245, 195, 109);
    private static readonly Font FuenteBoton = new("Segoe UI Semibold", 10, FontStyle.Bold);
    private static readonly Font FuenteCabecera = new("Segoe UI Semibold", 11, FontStyle.Bold);

    
// Ejecuta Aplicar.
    internal static void Aplicar(Control control, bool monocromo = false)
    {
        control.ForeColor = monocromo ? Color.Black : Texto;
        control.BackColor = monocromo ? Color.White : control is Form ? Fondo : Panel;
        if (!monocromo && control is Panel) control.BackColor = Color.Transparent;
        if (control is Button boton)
        {
            boton.FlatStyle = FlatStyle.Flat;
            boton.UseVisualStyleBackColor = false;
            boton.FlatAppearance.BorderColor = monocromo ? Color.Black : Color.FromArgb(96, 86, 85);
            boton.FlatAppearance.BorderSize = 1;
            boton.BackColor = monocromo ? Color.Black : Panel;
            boton.ForeColor = monocromo ? Color.White : Texto;
            boton.FlatAppearance.MouseOverBackColor = monocromo ? Color.FromArgb(50, 50, 50) : Color.FromArgb(63, 37, 44);
            boton.FlatAppearance.MouseDownBackColor = monocromo ? Color.FromArgb(75, 75, 75) : Color.FromArgb(106, 34, 45);
            boton.Font = FuenteBoton;
            boton.Cursor = Cursors.Hand;
            boton.Padding = new Padding(4);
        }
        if (control is TextBox caja) caja.BorderStyle = BorderStyle.FixedSingle;
        foreach (Control hijo in control.Controls) Aplicar(hijo, monocromo);
    }

    
// Ejecuta AccionPrincipal.
    internal static void AccionPrincipal(Button boton)
    {
        boton.BackColor = Acento;
        boton.ForeColor = Color.White;
        boton.FlatAppearance.BorderColor = Acento;
        boton.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 64, 79);
        boton.FlatAppearance.MouseDownBackColor = Color.FromArgb(157, 31, 43);
    }

    
// Ejecuta Cabecera.
    internal static void Cabecera(Label etiqueta)
    {
        etiqueta.Font = FuenteCabecera;
        etiqueta.Padding = new Padding(8);
        etiqueta.BackColor = Panel;
        etiqueta.ForeColor = Texto;
    }
}
