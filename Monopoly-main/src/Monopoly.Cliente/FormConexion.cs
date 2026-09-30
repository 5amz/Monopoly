using System;
using System.Drawing;
using System.Windows.Forms;
using Monopoly.Nucleo;
using Monopoly.Protocolo;

namespace Monopoly.Cliente;

public sealed class FormConexion : Form, IVistaConexion
{
    private readonly NumericUpDown cantidadJugadores = new()
    {
        Minimum = 1,
        Maximum = 4,
        Value = 4,
        Width = 55
    };
    private readonly CheckBox alojarAqui = new()
    {
        Text = "Crear partida en esta computadora",
        Checked = true,
        AutoSize = true
    };
    private readonly TextBox ip = new()
    {
        Text = "192.168.0.10",
        Dock = DockStyle.Fill,
        Enabled = false
    };
    private readonly TextBox puerto = new()
    {
        Text = "5000",
        Dock = DockStyle.Fill
    };
    private readonly Label estado = new()
    {
        Text = "Complete los nombres y pulse Conectar esta pantalla.",
        Dock = DockStyle.Fill,
        AutoSize = true
    };
    private readonly TextBox sala = new()
    {
        Multiline = true,
        ReadOnly = true,
        Dock = DockStyle.Fill,
        ScrollBars = ScrollBars.Vertical
    };
    private readonly Button iniciar = new BotonCiudad()
    {
        Text = "Conectar esta pantalla",
        Dock = DockStyle.Fill,
        Height = 44
    };
    private readonly CheckBox usarPico = new()
    {
        Text = "Usar Pico (dados y RFID)",
        Checked = true,
        AutoSize = true
    };
    private readonly TextBox puertoSerial = new()
    {
        Text = "COM3",
        Width = 85
    };
    private readonly TextBox nombreJ1 = new() { Text = "Jugador 1", Width = 250 };
    private readonly TextBox nombreJ2 = new() { Text = "Jugador 2", Width = 250 };
    private readonly TextBox nombreJ3 = new() { Text = "Jugador 3", Width = 250 };
    private readonly TextBox nombreJ4 = new() { Text = "Jugador 4", Width = 250 };
    private readonly FlowLayoutPanel hardware;
    private readonly Label[] etiquetasNombre = new Label[4];
    private readonly Label descripcion = new() { AutoSize = true };
    private ControladorMesaConexion controladorMesa = null !;
    private readonly Action abrir;
    private readonly Action cerrar;
    
// Crea el objeto.
    public FormConexion(Action abrirJuego, Action cerrarAplicacion)
    {
        abrir = abrirJuego;
        cerrar = cerrarAplicacion;
        Text = "Ciudad de Canciones · Conexión";
        ClientSize = new Size(660, 700);
        MinimumSize = new Size(620, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        var tabla = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(22), ColumnCount = 2, RowCount = 16
        };
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var titulo = new Label { Text = "Ciudad de Canciones", AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold), Margin = new Padding(0, 0, 0, 16) };
        tabla.Controls.Add(titulo, 0, 0);
        tabla.SetColumnSpan(titulo, 2);
        tabla.Controls.Add(new Label { Text = "Jugadores aquí", AutoSize = true }, 0, 1);
        tabla.Controls.Add(cantidadJugadores, 1, 1);
        TextBox[] nombres = { nombreJ1, nombreJ2, nombreJ3, nombreJ4 };
        for (int i = 0; i < nombres.Length; i++)
        {
            etiquetasNombre[i] = new Label { Text = "Nombre " + (i + 1), AutoSize = true, Margin = new Padding(3, 7, 3, 3) };
            nombres[i].Dock = DockStyle.Fill;
            nombres[i].Margin = new Padding(3, 3, 3, 6);
            tabla.Controls.Add(etiquetasNombre[i], 0, i + 2);
            tabla.Controls.Add(nombres[i], 1, i + 2);
        }
        alojarAqui.Margin = new Padding(3, 12, 3, 10);
        tabla.Controls.Add(alojarAqui, 0, 6);
        tabla.SetColumnSpan(alojarAqui, 2);
        tabla.Controls.Add(new Label { Text = "IP del servidor", AutoSize = true }, 0, 7);
        tabla.Controls.Add(ip, 1, 7);
        tabla.Controls.Add(new Label { Text = "Puerto", AutoSize = true }, 0, 8);
        tabla.Controls.Add(puerto, 1, 8);
        hardware = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        hardware.Controls.Add(usarPico);
        hardware.Controls.Add(new Label { Text = "Puerto USB:", AutoSize = true, Margin = new Padding(3, 6, 3, 3) });
        hardware.Controls.Add(puertoSerial);
        tabla.Controls.Add(hardware, 0, 9);
        tabla.SetColumnSpan(hardware, 2);
        descripcion.MaximumSize = new Size(550, 0);
        descripcion.Margin = new Padding(3, 6, 3, 12);
        tabla.Controls.Add(descripcion, 0, 10);
        tabla.SetColumnSpan(descripcion, 2);
        iniciar.Margin = new Padding(3, 4, 3, 10);
        tabla.Controls.Add(iniciar, 0, 11);
        tabla.SetColumnSpan(iniciar, 2);
        estado.MaximumSize = new Size(550, 0);
        tabla.Controls.Add(estado, 0, 12);
        tabla.SetColumnSpan(estado, 2);
        var ayudaImagenes = new LinkLabel { Text = "Ver carpeta de imágenes y archivos faltantes", AutoSize = true, LinkColor = Color.Black, ActiveLinkColor = Color.Black, VisitedLinkColor = Color.Black, Margin = new Padding(3, 8, 3, 8) };
        ayudaImagenes.LinkClicked += (_, _) => MostrarInformeImagenes();
        tabla.Controls.Add(ayudaImagenes, 0, 13);
        tabla.SetColumnSpan(ayudaImagenes, 2);
        tabla.Controls.Add(new Label { Text = "Sala · La partida empieza al conectar 4 jugadores", AutoSize = true }, 0, 14);
        tabla.SetColumnSpan(tabla.GetControlFromPosition(0, 14)!, 2);
        tabla.Controls.Add(sala, 0, 15);
        tabla.SetColumnSpan(sala, 2);
        for (int i = 0; i < 15; i++) tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sala.MinimumSize = new Size(0, 65);
        Controls.Add(tabla);
        AcceptButton = iniciar;
        usarPico.CheckedChanged += (_, _) => puertoSerial.Enabled = usarPico.Checked && iniciar.Enabled;
        iniciar.Click += Iniciar_Click;
        cantidadJugadores.ValueChanged += (_, _) => ActualizarNombresVisibles();
        alojarAqui.CheckedChanged += (_, _) => ActualizarModo();
        ActualizarNombresVisibles();
        ActualizarModo();
        TemaCiudad.Aplicar(this, monocromo: true);
    }

    
// Ejecuta MostrarInformeImagenes.
    private void MostrarInformeImagenes()
    {
        using var informe = new Form { Text = "Imágenes", ClientSize = new Size(700, 480), StartPosition = FormStartPosition.CenterParent };
        informe.Controls.Add(new TextBox
        {
            Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both,
            Text = "Carpeta usada:\r\n" + RecursosVista.Carpeta + "\r\n\r\n" +
                (RecursosVista.Informe.Length == 0 ? "Todas las imágenes están disponibles." : RecursosVista.Informe) +
                "\r\n\r\nLos archivos ausentes se omiten sin sustitutos. Reinicia el cliente después de cambiar imágenes."
        });
        TemaCiudad.Aplicar(informe, monocromo: true);
        informe.ShowDialog(this);
    }

    
// Ejecuta ActualizarNombresVisibles.
    private void ActualizarNombresVisibles()
    {
        int cantidad = (int)cantidadJugadores.Value;
        nombreJ1.Visible = cantidad >= 1;
        nombreJ2.Visible = cantidad >= 2;
        nombreJ3.Visible = cantidad >= 3;
        nombreJ4.Visible = cantidad >= 4;
        for (int i = 0; i < etiquetasNombre.Length; i++) etiquetasNombre[i].Visible = i < cantidad;
    }

    
// Ejecuta ActualizarModo.
    private void ActualizarModo()
    {
        bool aloja = alojarAqui.Checked;
        ip.Enabled = !aloja;
        hardware.Visible = aloja;
        descripcion.Text = aloja
            ? "Para unirse desde otra computadora, usa la IP de esta. Desmarca Pico solo para pruebas sin hardware."
            : "Escribe la IP de la computadora que creó la partida. El Pico se conecta allí.";
    }

    
// Ejecuta VincularMesa.
    public void VincularMesa(ControladorMesaConexion valor)
    {
        controladorMesa = valor;
    }

    
// Ejecuta NombresActivos.
    private string[] NombresActivos()
    {
        int cantidad = (int)cantidadJugadores.Value;
        var textos = new[] { nombreJ1.Text, nombreJ2.Text, nombreJ3.Text, nombreJ4.Text };
        var activos = new string[cantidad];
        Array.Copy(textos, activos, cantidad);
        return activos;
    }

    
// Ejecuta Iniciar_Click.
    private void Iniciar_Click(object? sender, EventArgs e)
    {
        if (alojarAqui.Checked)
        {
            controladorMesa.AlojarMesa(NombresActivos(), puerto.Text, usarPico.Checked, puertoSerial.Text.Trim());
        }
        else
        {
            controladorMesa.UnirseMesa(NombresActivos(), ip.Text, puerto.Text);
        }
    }

    
// Ejecuta MostrarConexion.
    public void MostrarConexion(string texto, bool puedeConectar)
    {
        estado.Text = texto;
        iniciar.Enabled = puedeConectar;
        cantidadJugadores.Enabled = puedeConectar;
        alojarAqui.Enabled = puedeConectar;
        ip.Enabled = puedeConectar && !alojarAqui.Checked;
        usarPico.Enabled = puedeConectar;
        puerto.Enabled = puedeConectar;
        puertoSerial.Enabled = puedeConectar && usarPico.Checked;
        nombreJ1.Enabled = nombreJ2.Enabled = nombreJ3.Enabled = nombreJ4.Enabled = puedeConectar;
    }

    
// Ejecuta MostrarSala.
    public void MostrarSala(string jugadores)
    {
        sala.Text = jugadores;
    }

    
// Ejecuta AbrirJuego.
    public void AbrirJuego()
    {
        abrir();
    }

    
// Ejecuta ConfirmarCierre.
    public void ConfirmarCierre(string texto, Action<bool> respuesta)
    {
        respuesta(MessageBox.Show(texto, "Cerrar partida", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes);
    }

    
// Ejecuta CerrarAplicacion.
    public void CerrarAplicacion()
    {
        cerrar();
    }
}
