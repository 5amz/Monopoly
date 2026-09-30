using System.Drawing;
using System.Windows.Forms;

namespace Monopoly.Cliente;

internal sealed class BotonCiudad : Button
{
    private bool encima;
    private bool pulsado;
    // Resalta el botón al entrar el cursor.
    protected override void OnMouseEnter(System.EventArgs e) { encima = true; base.OnMouseEnter(e); Invalidate(); }
    // Quita el resaltado al salir el cursor.
    protected override void OnMouseLeave(System.EventArgs e) { encima = pulsado = false; base.OnMouseLeave(e); Invalidate(); }
    // Marca el botón al presionarlo.
    protected override void OnMouseDown(MouseEventArgs e) { pulsado = e.Button == MouseButtons.Left; base.OnMouseDown(e); Invalidate(); }
    // Quita la marca al soltar el botón.
    protected override void OnMouseUp(MouseEventArgs e) { pulsado = false; base.OnMouseUp(e); Invalidate(); }
    // Actualiza el botón cuando cambia su estado.
    protected override void OnEnabledChanged(System.EventArgs e) { pulsado = false; base.OnEnabledChanged(e); Invalidate(); }
    // Marca el botón al pulsar la barra espaciadora.
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) pulsado = true; base.OnKeyDown(e); Invalidate(); }
    // Quita la marca al soltar la tecla.
    protected override void OnKeyUp(KeyEventArgs e) { pulsado = false; base.OnKeyUp(e); Invalidate(); }

    // Dibuja el control en la pantalla.
    protected override void OnPaint(PaintEventArgs e)
    {
        Color fondo = !Enabled ? Color.FromArgb(37, 38, 44) : pulsado ? FlatAppearance.MouseDownBackColor : encima ? FlatAppearance.MouseOverBackColor : BackColor;
        Color texto = Enabled ? ForeColor : Color.FromArgb(157, 158, 165);
        using var brocha = new SolidBrush(fondo);
        using var borde = new Pen(Enabled ? FlatAppearance.BorderColor : Color.FromArgb(68, 68, 76));
        e.Graphics.FillRectangle(brocha, ClientRectangle);
        e.Graphics.DrawRectangle(borde, 0, 0, Width - 1, Height - 1);
        TextRenderer.DrawText(e.Graphics, Text, Font, Rectangle.Inflate(ClientRectangle, -6, -2), texto,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues && Enabled)
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -4, -4), texto, fondo);
    }
}
