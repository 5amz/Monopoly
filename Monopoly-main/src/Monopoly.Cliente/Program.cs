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
    
// Ejecuta Main.
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
    
// Crea el objeto.
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

    
// Ejecuta ConfigurarHardware.
    private void ConfigurarHardware(bool usarPico, string puerto)
    {
        opciones.UsarHardware = usarPico;
        opciones.PuertoSerial = puerto;
    }

    
// Ejecuta AbrirJuego.
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

    
// Ejecuta SolicitarCierre.
    private void SolicitarCierre(object? sender, FormClosingEventArgs e)
    {
        if (!cerrando)
        {
            e.Cancel = true;
            controladorMesa.SolicitarCierre();
        }
    }

    
// Ejecuta OcultarHistorial.
    private void OcultarHistorial(object? sender, FormClosingEventArgs e)
    {
        if (!cerrando)
        {
            e.Cancel = true;
            historial.Hide();
        }
    }

    
// Ejecuta Cerrar.
    private void Cerrar()
    {
        cerrando = true;
        juego.DetenerReloj();
        historial.Close();
        juego.Close();
        conexion.Close();
        ExitThread();
    }

    
// Ejecuta Dispose.
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

    
// Crea el objeto.
    public ServidorLocalCliente(ConfiguracionPartida opciones)
    {
        this.opciones = opciones;
    }

    
// Ejecuta Iniciar.
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

    
// Ejecuta InyectarSerialSimulado.
    public bool InyectarSerialSimulado(string linea)
    {
        lock (bloqueo)
        {
            return activo && !UsarHardware && actual is not null && actual.RecibirLineaSerial(linea);
        }
    }

    
// Ejecuta ExportarTransacciones.
    public string ExportarTransacciones(string ruta)
    {
        lock (bloqueo)
        {
            if (!activo || actual is null) throw new InvalidOperationException("El servidor local no está activo.");
            return actual.ExportarTransacciones(ruta);
        }
    }

    
// Ejecuta Detener.
    public void Detener()
    {
        lock (bloqueo)
        {
            activo = false;
            actual?.Detener();
        }
    }

    
// Ejecuta Dispose.
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
