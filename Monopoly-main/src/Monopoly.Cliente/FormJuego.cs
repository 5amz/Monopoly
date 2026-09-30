using System;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Monopoly.Nucleo;
using Monopoly.Protocolo;

namespace Monopoly.Cliente;
public sealed class FormJuego : Form, IVistaJuego
{
    private IControladorJuego controlador = null !;
    private readonly TableroControl tablero = new();
    private readonly Label turnoActual = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label contadorTurnos = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
    private readonly Label[] jugadores = new Label[4];
    private readonly ToolTip detalles = new();
    private readonly TextBox log = new()
    {
        Multiline = true,
        ReadOnly = true,
        Dock = DockStyle.Fill,
        ScrollBars = ScrollBars.Vertical
    };
    private readonly Label estado = new()
    {
        Dock = DockStyle.Bottom,
        Height = 32,
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label bannerPago = new()
    {
        Dock = DockStyle.Top,
        Height = 48,
        TextAlign = ContentAlignment.MiddleCenter,
        BackColor = Color.OrangeRed,
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 13, FontStyle.Bold),
        Visible = false
    };
    private readonly Button abrirRegistro = new BotonCiudad { Text = "≡", Width = 38, Height = 30, AccessibleName = "Abrir registro" };
    private readonly Button silenciar = new BotonCiudad { Text = "Silenciar", Dock = DockStyle.Top, Height = 38 };
    private readonly Button fondoMusical = new BotonCiudad { Text = "Fondo: no", Dock = DockStyle.Top, Height = 38 };
    private readonly StringBuilder informeMultimedia = new();
    private readonly ControladorMultimedia multimedia;
    private Form? ventanaRegistro;
    private string textoRegistro = "";
    private string resultadoFinal = "";
    private bool detenida;
    private readonly Button comprar = new BotonCiudad()
    {
        Text = "Comprar",
        AutoSize = true,
        Enabled = false
    };
    private readonly Button noComprar = new BotonCiudad()
    {
        Text = "No comprar",
        AutoSize = true,
        Enabled = false
    };
    private readonly Button terminar = new BotonCiudad()
    {
        Text = "Terminar turno",
        AutoSize = true,
        Enabled = false
    };
    private readonly Button historial = new BotonCiudad()
    {
        Text = "Historial",
        AutoSize = true,
        Enabled = false
    };
    private readonly Button simularTarjeta = new BotonCiudad()
    {
        Text = "SIMULADOR · tarjeta RFID",
        AutoSize = true,
        Visible = false
    };
    private readonly Button simularDados = new BotonCiudad()
    {
        Text = "Tirar dados (prueba)", AutoSize = true, Visible = false, Enabled = false,
        Dock = DockStyle.Fill
    };
    private bool modoSimuladorConectado;
    private bool puedeTirarDados;
    private readonly Button exportar = new BotonCiudad()
    {
        Text = "TXT",
        AutoSize = true,
        Visible = false
    };
    private Func<string, bool>? inyectarSerial;
    private Func<string, string>? exportarTransacciones;
    private string jugadorEnTurno = "J1";
    private readonly Timer reloj = new()
    {
        Interval = 16
    };
    
// Crea el objeto.
    public FormJuego()
    {
        InformarMultimedia("Configuración de música: " + Path.Combine(AppContext.BaseDirectory, "Musica", "musica.json"));
        multimedia = new ControladorMultimedia(Path.Combine(AppContext.BaseDirectory, "Musica"), InformarMultimedia, new AudioWindows(InformarMultimedia));
        Text = PersonalizacionCiudad.Titulo;
        WindowState = FormWindowState.Maximized;
        ClientSize = new Size(1480, 980);
        MinimumSize = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        var contenido = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12)
        };
        contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        contenido.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        var cabecera = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1 };
        cabecera.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        cabecera.Controls.Add(turnoActual, 0, 0);
        for (int i = 0; i < jugadores.Length; i++)
        {
            cabecera.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
            jugadores[i] = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoEllipsis = true };
            cabecera.Controls.Add(jugadores[i], i + 1, 0);
        }
        cabecera.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
        cabecera.Controls.Add(contadorTurnos, 5, 0);
        contenido.Controls.Add(cabecera, 0, 0);
        bannerPago.Dock = DockStyle.Fill;
        bannerPago.AutoSize = true;
        bannerPago.Padding = new Padding(8);
        contenido.Controls.Add(bannerPago, 0, 1);
        var division = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        division.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        division.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        division.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));
        var lateral = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
        lateral.Controls.Add(abrirRegistro);
        detalles.SetToolTip(abrirRegistro, "Abrir el registro de la partida");
        var leyenda = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(8), AutoScroll = true };
        leyenda.Controls.Add(new Label { Text = "TIPOS DE CASILLA", Width = 144, Height = 32, TextAlign = ContentAlignment.MiddleLeft });
        foreach (string tipo in new[] { "P   Propiedad", "?   Carta de evento", "−   Impuesto", ">   Inicio / premio", "=   Descanso / visita" })
            leyenda.Controls.Add(new Label { Text = tipo, Width = 144, Height = 30, TextAlign = ContentAlignment.MiddleLeft });
        leyenda.Controls.Add(new Label { Text = "Alq. = alquiler", Width = 144, Height = 30 });
        foreach (var boton in new[] { fondoMusical, silenciar, simularTarjeta, simularDados })
        {
            boton.Dock = DockStyle.None; boton.AutoSize = false; boton.Width = 144; boton.Height = 48;
            leyenda.Controls.Add(boton);
        }
        simularTarjeta.Text = "Tarjeta de prueba";
        division.Controls.Add(lateral, 0, 0);
        division.Controls.Add(tablero, 1, 0);
        division.Controls.Add(leyenda, 2, 0);
        contenido.Controls.Add(division, 0, 2);
        var acciones = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1, Padding = new Padding(0, 6, 0, 0) };
        acciones.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        acciones.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        acciones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        acciones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        acciones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        acciones.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
        acciones.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        Button[] botones = { historial, comprar, noComprar, terminar, exportar };
        int[] columnas = { 0, 1, 2, 3, 5 };
        for (int i = 0; i < botones.Length; i++)
        {
            botones[i].AutoSize = false;
            botones[i].Dock = DockStyle.Fill;
            botones[i].Margin = new Padding(5);
            acciones.Controls.Add(botones[i], columnas[i], 0);
        }
        detalles.SetToolTip(exportar, "Exportar el historial oficial del servidor a TXT (solo anfitrión).");
        contenido.Controls.Add(acciones, 0, 3);
        Controls.Add(contenido);
        Controls.Add(estado);
        abrirRegistro.Click += (_, _) => AbrirRegistro();
        silenciar.Click += (_, _) => { multimedia.AlternarSilencio(); };
        fondoMusical.Click += (_, _) => { multimedia.AlternarFondo(); };
        comprar.Click += Comprar_Click;
        noComprar.Click += NoComprar_Click;
        terminar.Click += Terminar_Click;
        historial.Click += Historial_Click;
        simularTarjeta.Click += SimularTarjeta_Click;
        simularDados.Click += (_, _) =>
        {
            if (modoSimuladorConectado && puedeTirarDados) controlador.SolicitarTirarDados();
        };
        exportar.Click += Exportar_Click;
        TemaCiudad.Aplicar(this);
        BackgroundImage = RecursosVista.Obtener("fondo_tablero");
        TemaCiudad.AccionPrincipal(comprar);
        TemaCiudad.AccionPrincipal(terminar);
        TemaCiudad.Cabecera(turnoActual);
        TemaCiudad.Cabecera(contadorTurnos);
        foreach (var jugador in jugadores) TemaCiudad.Cabecera(jugador);
        estado.BackColor = TemaCiudad.Fondo;
        estado.Padding = new Padding(12, 0, 0, 0);
        log.ForeColor = TemaCiudad.Texto;
        log.BackColor = TemaCiudad.Panel;
        detalles.SetToolTip(fondoMusical, "Activa o desactiva solo tu canción de fondo. No cambia las canciones de las casillas.");
        detalles.SetToolTip(silenciar, "Silencia toda la música sin cambiar la opción del fondo.");
        detalles.SetToolTip(simularDados, "Sustituye el pulsador del Pico solo en modo SIMULADOR. Primero identifica al jugador con la tarjeta de prueba.");
    }

    
// Ejecuta ConfigurarHerramientasLocales.
    public void ConfigurarHerramientasLocales(bool anfitrion, bool usarHardware,
        Func<string, bool> recibirLineaSerial, Func<string, string> guardarTransacciones)
    {
        inyectarSerial = anfitrion && !usarHardware ? recibirLineaSerial : null;
        exportarTransacciones = anfitrion ? guardarTransacciones : null;
        exportar.Visible = exportarTransacciones is not null;
        if (anfitrion)
            Text = PersonalizacionCiudad.Titulo + (usarHardware ? " · Organizador / Pico" : " · Organizador / SIMULADOR");
    }

    
// Ejecuta SimularTarjeta_Click.
    private void SimularTarjeta_Click(object? sender, EventArgs e)
    {
        using var dialogo = new FormTarjetaSimulada(
            uid => { if (inyectarSerial is not null) inyectarSerial("RFID|" + uid); else controlador.SimularTarjetaRemota(uid); },
            jugadorEnTurno);
        dialogo.ShowDialog(this);
    }

    
// Ejecuta Exportar_Click.
    private void Exportar_Click(object? sender, EventArgs e)
    {
        if (exportarTransacciones is null) return;
        using var destino = new SaveFileDialog
        {
            Title = "Exportar historial oficial del servidor",
            Filter = "Archivo de transacciones (*.txt)|*.txt",
            DefaultExt = "txt",
            AddExtension = true,
            FileName = "transacciones-partida.txt"
        };
        if (destino.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            string ruta = exportarTransacciones(destino.FileName);
            MessageBox.Show(this, "Historial oficial exportado a:\n" + ruta, "Exportar TXT",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            MostrarError("No se pudo exportar", error.Message);
        }
    }

    
// Ejecuta Vincular.
    public void Vincular(IControladorJuego juego)
    {
        controlador = juego;
        tablero.Vincular(juego);
        multimedia.Vincular(juego.Motor, () => juego.Estado);
        reloj.Tick += (_, _) =>
        {
            multimedia.Actualizar();
                foreach (var jugador in jugadores) jugador.Invalidate();
            tablero.Invalidate();
        };
        reloj.Start();
    }

    
// Ejecuta Comprar_Click.
    private void Comprar_Click(object? sender, EventArgs e)
    {
        controlador.SolicitarComprar();
    }

    
// Ejecuta NoComprar_Click.
    private void NoComprar_Click(object? sender, EventArgs e)
    {
        controlador.RechazarCompra();
    }

    
// Ejecuta Terminar_Click.
    private void Terminar_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show(this, "¿Terminar el turno de " + jugadorEnTurno + "?", "Terminar turno",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            controlador.TerminarTurno();
        }
    }

    
// Ejecuta Historial_Click.
    private void Historial_Click(object? sender, EventArgs e)
    {
        controlador.AbrirHistorial();
    }

    
// Ejecuta MostrarEscena.
    public void MostrarEscena(EscenaTablero escena)
    {
        foreach (var elemento in escena.Elementos)
        {
            if (elemento.Tipo != "CONTROL") continue;
            if (elemento.Destino == "silenciar") silenciar.Text = elemento.Contenido;
            else if (elemento.Destino == "fondo") fondoMusical.Text = elemento.Contenido;
        }
        tablero.Mostrar(escena);
    }

    
// Ejecuta MostrarJugadores.
    public void MostrarJugadores(ListaSimple<JugadorVista> datos, string idEnTurno)
    {
        jugadorEnTurno = idEnTurno;
        string nombreActual = idEnTurno;
        for (int i = 0; i < jugadores.Length; i++)
        {
            string id = "J" + (i + 1);
            if (!datos.Buscar(j => j.Id == id, out var jugador))
            {
                jugadores[i].Text = id + "\nSin conectar";
                continue;
            }
            if (id == idEnTurno) nombreActual = jugador.Nombre;
            jugadores[i].Text = jugador.Nombre + "\n₡" + jugador.Saldo.ToString("N0") + (jugador.Activo ? "" : " · Fuera");
            jugadores[i].BorderStyle = BorderStyle.None;
            jugadores[i].BackColor = id == idEnTurno ? Color.FromArgb(66, 27, 35) : TemaCiudad.Panel;
            jugadores[i].ForeColor = !jugador.Activo ? Color.Gray : id == idEnTurno ? TemaCiudad.Destacado : TemaCiudad.Texto;
            detalles.SetToolTip(jugadores[i], id + " · " + jugador.Nombre + "\nPosición: " + jugador.Posicion +
                "\nPropiedades: " + string.Join(", ", jugador.Propiedades));
        }
        turnoActual.Text = "Turno de:\n" + nombreActual;
    }

    
// Ejecuta MostrarDados.
    public void MostrarDados(int uno, int dos, int total, bool esHardware)
    {
        detalles.SetToolTip(tablero, total == 0 ? "Esperando dados" : "Total: " + total);
    }

    
// Ejecuta MostrarEstadoBotones.
    public void MostrarEstadoBotones(EstadoBotones valor)
    {
        comprar.Enabled = valor.PuedeComprar;
        noComprar.Enabled = valor.PuedeNoComprar;
        terminar.Enabled = valor.PuedeTerminarTurno;
        historial.Enabled = valor.PuedeVerHistorial;
        puedeTirarDados = valor.PuedeTirarDados;
        simularDados.Enabled = modoSimuladorConectado && puedeTirarDados;
    }

    
// Ejecuta AgregarLineaLog.
    public void AgregarLineaLog(string linea)
    {
        textoRegistro = linea;
        log.Text = textoRegistro + "\r\nMULTIMEDIA\r\n" + informeMultimedia;
        log.SelectionStart = log.TextLength;
        log.ScrollToCaret();
    }

    
// Ejecuta MostrarCarta.
    public void MostrarCarta(string cancion, string texto, string efecto, int valor)
    {
    }

    
// Ejecuta MostrarError.
    public void MostrarError(string codigo, string mensaje)
    {
        MessageBox.Show(this, mensaje, codigo, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    
// Ejecuta MostrarFinPartida.
    public void MostrarFinPartida(ResultadoPartida resultado)
    {
        resultadoFinal = "GANADOR: " + resultado.Nombre + " (" + resultado.IdGanador + ") · Patrimonio: ₡" + resultado.Patrimonio.ToString("N0") + " · " + resultado.Motivo;
        OcultarPagoPendiente();
    }

    
// Ejecuta MostrarEstadoConexion.
    public void MostrarEstadoConexion(EstadoConexion valor)
    {
        estado.Text = valor.Texto;
        contadorTurnos.Text = "Turno " + valor.Turno + "/" + valor.MaxTurnos;
        modoSimuladorConectado = valor.Conectado && valor.Modo == "SIMULADOR";
        simularTarjeta.Visible = modoSimuladorConectado;
        simularDados.Visible = modoSimuladorConectado;
        simularDados.Enabled = modoSimuladorConectado && puedeTirarDados;
    }

    
// Ejecuta MostrarPagoPendiente.
    public void MostrarPagoPendiente(string idJugador, string descripcion, bool tarjetaRechazada)
    {
        if (tarjetaRechazada)
        {
            bannerPago.BackColor = Color.FromArgb(137, 29, 41);
            bannerPago.ForeColor = Color.White;
            bannerPago.Text = "TARJETA INCORRECTA, RECHAZADA — VUELVA A INTENTAR. Esperando tarjeta de " + idJugador + " para: " + descripcion;
        }
        else
        {
            bannerPago.BackColor = Color.FromArgb(61, 44, 27);
            bannerPago.ForeColor = TemaCiudad.Destacado;
            bannerPago.Text = "ESPERANDO TARJETA DE " + idJugador + " PARA CONFIRMAR: " + descripcion;
        }

        bannerPago.Visible = true;
    }

    
// Ejecuta OcultarPagoPendiente.
    public void OcultarPagoPendiente()
    {
        bannerPago.Visible = resultadoFinal.Length > 0;
        if (resultadoFinal.Length > 0)
        {
            bannerPago.Text = resultadoFinal;
            bannerPago.BackColor = TemaCiudad.Panel;
            bannerPago.ForeColor = TemaCiudad.Destacado;
        }
    }

    
// Ejecuta MostrarEfecto.
    public void MostrarEfecto(EfectoMultimedia efecto) => multimedia.Recibir(efecto);

    
// Ejecuta InformarMultimedia.
    private void InformarMultimedia(string texto)
    {
        informeMultimedia.AppendLine(texto);
        log.Text = textoRegistro + "\r\nMULTIMEDIA\r\n" + informeMultimedia;
    }

    
// Ejecuta AbrirRegistro.
    private void AbrirRegistro()
    {
        if (ventanaRegistro is null || ventanaRegistro.IsDisposed)
        {
            ventanaRegistro = new Form { Text = "Registro de la partida", Size = new Size(720, 440), StartPosition = FormStartPosition.CenterParent };
            ventanaRegistro.Controls.Add(log);
            ventanaRegistro.FormClosing += (_, e) => { e.Cancel = true; ventanaRegistro.Hide(); };
            TemaCiudad.Aplicar(ventanaRegistro);
        }
        ventanaRegistro.Show(this);
        ventanaRegistro.BringToFront();
    }

    
// Ejecuta OnPaintBackground.
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (BackgroundImage is not Image foto)
        {
            base.OnPaintBackground(e);
            return;
        }
        float escala = Math.Max((float)ClientSize.Width / foto.Width, (float)ClientSize.Height / foto.Height);
        float ancho = foto.Width * escala, alto = foto.Height * escala;
        e.Graphics.DrawImage(foto, (ClientSize.Width - ancho) / 2, (ClientSize.Height - alto) / 2, ancho, alto);
    }

    
// Ejecuta DetenerReloj.
    public void DetenerReloj()
    {
        if (detenida) return;
        detenida = true;
        reloj.Stop();
        reloj.Dispose();
        multimedia.Dispose();
    }
    
// Ejecuta Dispose.
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DetenerReloj();
            ventanaRegistro?.Dispose();
            log.Dispose();
            detalles.Dispose();
        }
        base.Dispose(disposing);
    }

}

internal sealed class FormTarjetaSimulada : Form
{
    
// Crea el objeto.
    public FormTarjetaSimulada(Action<string> enviarUid, string jugadorActual)
    {
        Text = "SIMULADOR RFID · administración local";
        ClientSize = new Size(480, 270);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 10);
        var contenido = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 4
        };
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        contenido.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "SIMULADOR para desarrollo.\nElija la tarjeta solicitada para el pago pendiente. El servidor valida la identidad, el turno y la operación."
        }, 0, 0);
        var tarjeta = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        string[] uids = { "62384551", "A98F8656", "D03B9032", "2203D734" };
        for (int i = 0; i < uids.Length; i++) tarjeta.Items.Add("J" + (i + 1) + " · " + uids[i]);
        tarjeta.SelectedIndex = jugadorActual switch { "J2" => 1, "J3" => 2, "J4" => 3, _ => 0 };
        contenido.Controls.Add(tarjeta, 0, 1);
        var enviar = new BotonCiudad { Text = "Acercar tarjeta de prueba", AutoSize = true };
        contenido.Controls.Add(enviar, 0, 2);
        var resultado = new Label { Dock = DockStyle.Fill, Text = "Solo se envía la tarjeta; los saldos los modifica el Banco." };
        contenido.Controls.Add(resultado, 0, 3);
        enviar.Click += (_, _) =>
        {
            enviarUid(uids[tarjeta.SelectedIndex]);
            resultado.Text = "Tarjeta enviada. Revise el aviso oficial de la partida.";
        };
        Controls.Add(contenido);
        TemaCiudad.Aplicar(this);
    }
}
