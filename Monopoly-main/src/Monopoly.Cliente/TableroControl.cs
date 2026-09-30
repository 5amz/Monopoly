using System;
using System.Windows.Forms;
using Monopoly.Nucleo;
using Monopoly.Protocolo;

namespace Monopoly.Cliente;
public sealed class TableroControl : Panel
{
    private EscenaTablero escena = new(new ListaSimple<ElementoEscena>().Congelar());
    private IControladorJuego? controlador;
    private readonly ToolTip detalle = new();
    private string ultimaPista = "";
    // Prepara el tablero para dibujar sin parpadeos.
    public TableroControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Dock = DockStyle.Fill;
    }

    // Guarda el controlador de esta vista.
    public void Vincular(IControladorJuego juego)
    {
        controlador = juego;
        controlador.CambiarTamano(ClientSize.Width, ClientSize.Height);
    }

    // Guarda la escena y pide dibujarla.
    public void Mostrar(EscenaTablero nueva)
    {
        escena = nueva;
        Invalidate();
    }

    // Avisa al controlador del nuevo tamaño del tablero.
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        controlador?.CambiarTamano(ClientSize.Width, ClientSize.Height);
    }

    // Muestra los datos de la casilla bajo el cursor.
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        string pista = "";
        foreach (var elemento in escena.Elementos)
        {
            var area = elemento.Area;
            if (elemento.Tipo == "PISTA" && e.X >= area.X && e.X < area.X + area.Ancho && e.Y >= area.Y && e.Y < area.Y + area.Alto)
            {
                pista = elemento.Contenido;
                break;
            }
        }

        if (pista != ultimaPista)
        {
            ultimaPista = pista;
            detalle.SetToolTip(this, pista);
        }
    }

    // Libera los recursos al cerrar.
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            detalle.Dispose();
        }

        base.Dispose(disposing);
    }

    // Dibuja el control en la pantalla.
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        PintorEscena.Pintar(e.Graphics, escena);
    }
}
