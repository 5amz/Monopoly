using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Monopoly.Nucleo;
using Monopoly.Integracion;
using Monopoly.Protocolo;

namespace Monopoly.Cliente;
internal static class Program
{
    // Inicia el programa.
    [STAThread]
    private static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("es-CR");
        ApplicationConfiguration.Initialize();
        try
        {
            RecursosVista.Cargar();
            using var ventanas = new CicloVentanas();
            Application.Run(ventanas);
        }
        catch (Exception error)when (error is FileNotFoundException or ArgumentException)
        {
            MessageBox.Show(error.Message, "Revise las imágenes", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            RecursosVista.Liberar();
        }
    }
}

internal sealed class CicloVentanas : ApplicationContext
{
    private readonly FormConexion conexion;
    private readonly FormJuego juego = new();
    private readonly FormTransacciones historial = new();
    private readonly ConfiguracionPartida opciones = new();
    private readonly ServidorLocalCliente servidor;
    private readonly ControladorMesaConexion controladorMesa;
    private bool vinculado;
    private bool cerrando;
    // Prepara las ventanas y conecta sus controladores.
    internal CicloVentanas()
    {
        servidor = new ServidorLocalCliente(opciones);
        conexion = new FormConexion(AbrirJuego, Cerrar);
        _ = conexion.Handle;
        var ui = new SincronizadorWindowsForms(conexion);
        var motor = new MotorAnimacion();

        controladorMesa = new ControladorMesaConexion(new AnfitrionServidor(servidor), conexion, juego, ui, motor);
        controladorMesa.PrepararServidor = ConfigurarHardware;
        controladorMesa.PartidaDisponible += () =>
        {
            if (vinculado) return;
            vinculado = true;
            juego.Vincular(controladorMesa.ControladorJuego !);
            historial.Vincular(controladorMesa.ControladorJuego !);
            controladorMesa.ControladorJuego !.VincularHistorial(historial);
        };

        conexion.VincularMesa(controladorMesa);
        conexion.FormClosing += SolicitarCierre;
        juego.FormClosing += SolicitarCierre;
        historial.FormClosing += OcultarHistorial;
        conexion.Show();
    }

    // Guarda la opción de Pico y su puerto.
    private void ConfigurarHardware(bool usarPico, string puerto)
    {
        opciones.UsarHardware = usarPico;
        opciones.PuertoSerial = puerto;
    }

    // Abre la ventana del juego.
    private void AbrirJuego()
    {
        juego.ConfigurarHerramientasLocales(servidor.Activo, servidor.UsarHardware,
            servidor.InyectarSerialSimulado, servidor.ExportarTransacciones);
        if (!juego.Visible)
        {
            juego.Show();
            conexion.Hide();
        }
    }

    // Pide confirmar el cierre de la aplicación.
    private void SolicitarCierre(object? sender, FormClosingEventArgs e)
    {
        if (!cerrando)
        {
            e.Cancel = true;
            controladorMesa.SolicitarCierre();
        }
    }

    // Oculta la ventana del historial.
    private void OcultarHistorial(object? sender, FormClosingEventArgs e)
    {
        if (!cerrando)
        {
            e.Cancel = true;
            historial.Hide();
        }
    }

    // Cierra las ventanas de la aplicación.
    private void Cerrar()
    {
        cerrando = true;
        juego.DetenerReloj();
        historial.Close();
        juego.Close();
        conexion.Close();
        ExitThread();
    }

    // Libera los recursos al cerrar.
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            conexion.Dispose();
            juego.Dispose();
            historial.Dispose();
            servidor.Dispose();
        }

        base.Dispose(disposing);
    }
}

internal sealed class ServidorLocalCliente : IServidorEmbebido, IDisposable
{
    private readonly object bloqueo = new();
    private readonly ConfiguracionPartida opciones;
    private ServidorIntegrado? actual;
    private volatile bool activo;
    private bool cerrado;
    public bool Activo => activo;
    public bool UsarHardware { get; private set; } = true;
    public event Action? ServidorListo;
    public event Action<string>? ServidorFallo;

    // Guarda las opciones del servidor local.
    public ServidorLocalCliente(ConfiguracionPartida opciones)
    {
        this.opciones = opciones;
    }

    // Inicia el servidor con las opciones elegidas.
    public void Iniciar(int puerto)
    {
        lock (bloqueo)
        {
            if (cerrado) throw new InvalidOperationException("El anfitrión ya se cerró.");
            if (activo) throw new InvalidOperationException("Ya hay un servidor local iniciado.");
            actual?.Dispose();
            var nuevo = new ServidorIntegrado(opciones);
            actual = nuevo;
            UsarHardware = opciones.UsarHardware;
            nuevo.ServidorListo += () =>
            {
                activo = true;
                ServidorListo?.Invoke();
            };
            nuevo.ServidorFallo += texto =>
            {
                activo = false;
                ServidorFallo?.Invoke(texto);
            };
            nuevo.Iniciar(puerto);
        }
    }

    // Envía una tarjeta al servidor en el modo sin Pico.
    public bool InyectarSerialSimulado(string linea)
    {
        lock (bloqueo)
        {
            return activo && !UsarHardware && actual is not null && actual.RecibirLineaSerial(linea);
        }
    }

    // Guarda las transacciones del servidor.
    public string ExportarTransacciones(string ruta)
    {
        lock (bloqueo)
        {
            if (!activo || actual is null) throw new InvalidOperationException("El servidor local no está activo.");
            return actual.ExportarTransacciones(ruta);
        }
    }

    // Detiene el recurso en uso.
    public void Detener()
    {
        lock (bloqueo)
        {
            activo = false;
            actual?.Detener();
        }
    }

    // Libera los recursos al cerrar.
    public void Dispose()
    {
        lock (bloqueo)
        {
            cerrado = true;
            activo = false;
            actual?.Dispose();
            actual = null;
        }
    }
}
