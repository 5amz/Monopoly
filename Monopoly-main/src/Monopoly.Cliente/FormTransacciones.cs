using System;
using System.Drawing;
using System.Windows.Forms;
using Monopoly.Nucleo;
using Monopoly.Protocolo;

namespace Monopoly.Cliente;
public sealed class FormTransacciones : Form, IVistaTransacciones
{
    private IControladorJuego controlador = null !;
    private readonly ComboBox jugadores = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 120
    };
    private readonly ComboBox tipos = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 210
    };
    private readonly Label texto = new()
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(24),
        Font = new Font("Segoe UI", 14)
    };
    private readonly Label contador = new()
    {
        AutoSize = true
    };
    private readonly Button anterior = new BotonCiudad()
    {
        Text = "Anterior",
        AutoSize = true
    };
    private readonly Button siguiente = new BotonCiudad()
    {
        Text = "Siguiente",
        AutoSize = true
    };
    // Prepara los filtros y botones del historial.
    public FormTransacciones()
    {
        Text = "Ciudad de Canciones · Historial";
        ClientSize = new Size(620, 380);
        StartPosition = FormStartPosition.CenterParent;
        var filtros = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(12)
        };
        var aplicar = new Button
        {
            Text = "Aplicar filtros",
            AutoSize = true
        };
        filtros.Controls.Add(jugadores);
        filtros.Controls.Add(tipos);
        filtros.Controls.Add(aplicar);
        var navegacion = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            Padding = new Padding(12)
        };
        var primero = new Button
        {
            Text = "Más antigua",
            AutoSize = true
        };
        var ultimo = new Button
        {
            Text = "Más reciente",
            AutoSize = true
        };
        navegacion.Controls.Add(primero);
        navegacion.Controls.Add(anterior);
        navegacion.Controls.Add(siguiente);
        navegacion.Controls.Add(ultimo);
        navegacion.Controls.Add(contador);
        Controls.Add(texto);
        Controls.Add(filtros);
        Controls.Add(navegacion);
        aplicar.Click += Aplicar_Click;
        anterior.Click += Anterior_Click;
        siguiente.Click += Siguiente_Click;
        primero.Click += Primero_Click;
        ultimo.Click += Ultimo_Click;
        TemaCiudad.Aplicar(this);
    }

    // Guarda el controlador de esta vista.
    public void Vincular(IControladorJuego juego)
    {
        controlador = juego;
    }

    // Aplica los filtros elegidos.
    private void Aplicar_Click(object? sender, EventArgs e)
    {
        controlador.FiltrarHistorial(jugadores.Text, tipos.Text);
    }

    // Pide la transacción anterior.
    private void Anterior_Click(object? sender, EventArgs e)
    {
        controlador.HistorialAnterior();
    }

    // Pide la siguiente transacción.
    private void Siguiente_Click(object? sender, EventArgs e)
    {
        controlador.HistorialSiguiente();
    }

    // Pide la transacción más antigua.
    private void Primero_Click(object? sender, EventArgs e)
    {
        controlador.HistorialPrimero();
    }

    // Pide la transacción más reciente.
    private void Ultimo_Click(object? sender, EventArgs e)
    {
        controlador.HistorialUltimo();
    }

    // Muestra la transacción seleccionada.
    public void MostrarHistorial(PaginaHistorial pagina)
    {
        texto.Text = pagina.Texto;
        contador.Text = pagina.Contador;
        anterior.Enabled = pagina.PuedeAnterior;
        siguiente.Enabled = pagina.PuedeSiguiente;
    }

    // Muestra las opciones para filtrar el historial.
    public void MostrarFiltros(ListaSimple<string> ids, ListaSimple<string> categorias)
    {
        string id = jugadores.Text;
        string tipo = tipos.Text;
        jugadores.Items.Clear();
        tipos.Items.Clear();
        foreach (string valor in ids)
        {
            jugadores.Items.Add(valor);
        }

        foreach (string valor in categorias)
        {
            tipos.Items.Add(valor);
        }

        jugadores.SelectedItem = id.Length == 0 ? "TODOS" : id;
        tipos.SelectedItem = tipo.Length == 0 ? "TODOS" : tipo;
    }

    // Abre la ventana del historial.
    public void Abrir()
    {
        Show();
        BringToFront();
    }
}
