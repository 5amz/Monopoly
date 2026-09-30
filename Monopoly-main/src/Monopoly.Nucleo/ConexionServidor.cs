using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;
public sealed class ConexionServidor : IDisposable
{
    private readonly object bloqueo = new();
    private readonly ColaCircular<Mensaje> salientes = new(256);
    private readonly AutoResetEvent despertar = new(false);
    private CancellationTokenSource? cancelacion;
    private TcpClient? cliente;
    private StreamWriter? escritor;
    private Thread? lectorHilo;
    private Thread? escritorHilo;
    private Timer? ping;
    private long ultimoPong;
    private bool dispuesto;
    public bool Reintentando { get; private set; }

    public event Action<Mensaje>? MensajeRecibido;
    public event Action<string>? ConexionPerdida;
    public event Action? ConexionEstablecida;
    public bool Conectado
    {
        get
        {
            lock (bloqueo)
            {
                return escritor is not null;
            }
        }
    }

    // Abre la conexión con el servidor.
    public void Conectar(string ip, int puerto)
    {
        Desconectar();
        lock (bloqueo)
        {
            ObjectDisposedException.ThrowIf(dispuesto, this);
            cancelacion = new CancellationTokenSource();
            Reintentando = true;
            var token = cancelacion.Token;
            escritorHilo = new Thread(() => Escribir(token))
            {
                IsBackground = true,
                Name = "Monopoly escritor"
            };
            lectorHilo = new Thread(() => Leer(ip, puerto, token))
            {
                IsBackground = true,
                Name = "Monopoly lector"
            };
            escritorHilo.Start();
            lectorHilo.Start();
        }
    }

    // Agrega el mensaje a la cola de envío.
    public bool Enviar(Mensaje mensaje)
    {
        _ = mensaje.ToString();
        lock (bloqueo)
        {
            if (escritor is null || cancelacion is null || cancelacion.IsCancellationRequested)
            {
                return false;
            }

            if (!salientes.Encolar(mensaje))
            {
                throw new InvalidOperationException("La cola de envío está llena.");
            }
        }

        despertar.Set();
        return true;
    }

    // Saca mensajes de la cola y los envía por el socket.
    private void Escribir(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            despertar.WaitOne(200);
            try
            {
                lock (bloqueo)
                {
                    while (!token.IsCancellationRequested && escritor is not null && salientes.Desencolar(out var m))
                    {
                        escritor.WriteLine(m.ToString());
                    }
                }
            }
            catch (Exception e)when (e is IOException or SocketException or ObjectDisposedException)
            {
                CerrarTransporte();
            }
        }
    }

    // Lee los mensajes que llegan por el socket.
    private void Leer(string ip, int puerto, CancellationToken token)
    {
        int intento = 0;
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (intento > 0 && token.WaitHandle.WaitOne(intento * 1000))
                {
                    break;
                }

                using var tcp = new TcpClient(AddressFamily.InterNetwork)
                {
                    NoDelay = true
                };
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(5000);
                tcp.ConnectAsync(IPAddress.Parse(ip), puerto, timeout.Token).AsTask().GetAwaiter().GetResult();
                var flujo = tcp.GetStream();
                flujo.WriteTimeout = 2000;
                flujo.ReadTimeout = 35000;
                using var entrada = new StreamReader(flujo, new UTF8Encoding(false, true), false, 4096, true);
                lock (bloqueo)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    cliente = tcp;
                    escritor = new StreamWriter(flujo, new UTF8Encoding(false), 4096, true)
                    {
                        AutoFlush = true,
                        NewLine = "\n"
                    };
                    salientes.Limpiar();
                    ultimoPong = Environment.TickCount64;
                }

                intento = 0;
                Reintentando = false;
                ping = new Timer(_ => Latido(), null, 10000, 10000);
                ConexionEstablecida?.Invoke();
                while (!token.IsCancellationRequested)
                {
                    string? linea = AnalizadorMensajes.LeerLinea(entrada);
                    if (linea is null)
                    {
                        throw new IOException("El servidor cerró la conexión.");
                    }

                    var mensaje = AnalizadorMensajes.Analizar(linea);
                    if (mensaje.Tipo == "PONG")
                    {
                        Interlocked.Exchange(ref ultimoPong, Environment.TickCount64);
                    }

                    MensajeRecibido?.Invoke(mensaje);
                }
            }
            catch (Exception e)when (e is IOException or SocketException or OperationCanceledException or FormatException or ObjectDisposedException or DecoderFallbackException)
            {
                CerrarTransporte();
                if (token.IsCancellationRequested)
                {
                    break;
                }

                intento++;
                Reintentando = intento <= 3;
                ConexionPerdida?.Invoke(intento <= 3 ? $"Conexión interrumpida. Reintento {intento}/3: {e.Message}" : "No se pudo recuperar la conexión. Puede volver a unirse.");
                if (intento > 3)
                {
                    break;
                }
            }
            finally
            {
                CerrarTransporte();
            }
        }
    }

    // Envía PING y comprueba si el servidor sigue respondiendo.
    private void Latido()
    {
        if (Environment.TickCount64 - Interlocked.Read(ref ultimoPong) > 30000)
        {
            CerrarTransporte();
            return;
        }

        try
        {
            Enviar(Mensaje.Crear("PING"));
        }
        catch (InvalidOperationException)
        {
            CerrarTransporte();
        }
    }

    // Cierra el socket y borra los mensajes pendientes.
    private void CerrarTransporte()
    {
        ping?.Dispose();
        ping = null;
        lock (bloqueo)
        {
            cliente?.Dispose();
            cliente = null;
            try
            {
                escritor?.Dispose();
            }
            catch (IOException)
            {
            }

            escritor = null;
            salientes.Limpiar();
        }
    }

    // Envía el aviso de salida antes de cerrar la conexión.
    public void EnviarCierre(Mensaje mensaje)
    {
        try
        {
            lock (bloqueo)
            {
                salientes.Limpiar();
                escritor?.WriteLine(mensaje.ToString());
            }
        }
        catch (Exception e)when (e is IOException or SocketException or ObjectDisposedException)
        {
        }
    }

    // Cancela los siguientes intentos de conexión.
    public void SuspenderReintentos()
    {
        cancelacion?.Cancel();
        Reintentando = false;
    }

    // Avisa de la salida y cierra la conexión.
    public void Desconectar()
    {
        var c = Interlocked.Exchange(ref cancelacion, null);
        if (c is null)
        {
            return;
        }

        c.Cancel();
        Reintentando = false;
        despertar.Set();
        CerrarTransporte();
        if (lectorHilo != Thread.CurrentThread)
        {
            lectorHilo?.Join(6000);
        }

        if (escritorHilo != Thread.CurrentThread)
        {
            escritorHilo?.Join(2500);
        }

        c.Dispose();
    }

    // Libera los recursos al terminar de usar el objeto.
    public void Dispose()
    {
        if (dispuesto)
        {
            return;
        }

        dispuesto = true;
        Desconectar();
        if (Thread.CurrentThread != lectorHilo && Thread.CurrentThread != escritorHilo)
        {
            despertar.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
