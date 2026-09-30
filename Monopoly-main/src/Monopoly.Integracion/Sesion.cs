using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Monopoly.Protocolo;

namespace Monopoly.Integracion;
internal sealed class Sesion
{
    private readonly TcpClient tcp;
    private readonly object bloqueo = new();
    private readonly ColaCircular<Mensaje> salida = new(256);
    private readonly AutoResetEvent señal = new(false);
    private volatile bool cerrada;
    private bool cerrarAlVaciar;
    private readonly ManualResetEventSlim cierreCompleto = new(false);
    private readonly Action<Sesion, Mensaje> recibir;
    private readonly Action<Sesion> desconectar;
    internal JugadorConectado Jugador;
    internal bool Cerrada => cerrada;

    
// Crea el objeto.
    internal Sesion(TcpClient tcp, Action<Sesion, Mensaje> recibir, Action<Sesion> desconectar)
    {
        this.tcp = tcp;
        this.recibir = recibir;
        this.desconectar = desconectar;
        tcp.NoDelay = true;
    }

    
// Ejecuta Iniciar.
    internal void Iniciar()
    {
        new Thread(Escribir)
        {
            IsBackground = true,
            Name = "Servidor escritor"
        }.Start();
        new Thread(Leer)
        {
            IsBackground = true,
            Name = "Servidor lector"
        }.Start();
    }

    
// Ejecuta Enviar.
    internal void Enviar(Mensaje m)
    {
        lock (bloqueo)
        {
            if (cerrada)
            {
                return;
            }

            if (!salida.Encolar(m))
            {
                Cerrar();
                return;
            }
        }

        señal.Set();
    }

    
// Ejecuta Escribir.
    private void Escribir()
    {
        try
        {
            using var escritor = new StreamWriter(tcp.GetStream(), new UTF8Encoding(false), 4096, true)
            {
                AutoFlush = true,
                NewLine = "\n"
            };
            tcp.GetStream().WriteTimeout = 2000;
            while (!cerrada)
            {
                señal.WaitOne(250);
                while (!cerrada)
                {
                    Mensaje m;
                    lock (bloqueo)
                    {
                        if (!salida.Desencolar(out m))
                        {
                            if (cerrarAlVaciar)
                            {
                                return;
                            }

                            break;
                        }
                    }

                    escritor.WriteLine(m.ToString());
                }
            }
        }
        catch (Exception e)when (e is IOException or SocketException or ObjectDisposedException or FormatException or InvalidOperationException)
        {
        }
        finally
        {
            Cerrar();
        }
    }

    
// Ejecuta Leer.
    private void Leer()
    {
        try
        {
            using var lector = new StreamReader(tcp.GetStream(), new UTF8Encoding(false, true), false, 4096, true);
            while (!cerrada)
            {
                string linea = AnalizadorMensajes.LeerLinea(lector);
                if (linea is null)
                {
                    break;
                }

                try
                {
                    recibir(this, AnalizadorMensajes.Analizar(linea));
                }
                catch (FormatException e)
                {
                    Enviar(ConstructorMensajes.Error("DESCONOCIDA", "FORMATO_INVALIDO", e.Message));
                }
                catch (Exception e) when (e is not (IOException or SocketException or ObjectDisposedException or DecoderFallbackException or InvalidOperationException))
                {
                    
                    
                    
                    Enviar(ConstructorMensajes.Error("DESCONOCIDA", "ACCION_INVALIDA", "Error interno al procesar la solicitud."));
                }
            }
        }
        catch (Exception e)when (e is IOException or SocketException or ObjectDisposedException or FormatException or DecoderFallbackException or InvalidOperationException)
        {
        }
        finally
        {
            Cerrar();
            desconectar(this);
        }
    }

    
// Ejecuta FinalizarConAviso.
    internal void FinalizarConAviso(Mensaje aviso)
    {
        lock (bloqueo)
        {
            if (cerrada)
            {
                return;
            }

            salida.Limpiar();
            salida.Encolar(aviso);
            cerrarAlVaciar = true;
        }

        señal.Set();
    }

    
// Ejecuta EsperarCierre.
    internal void EsperarCierre()
    {
        if (!cierreCompleto.Wait(2500))
        {
            Cerrar();
        }
    }

    
// Ejecuta Cerrar.
    internal void Cerrar()
    {
        cerrada = true;
        tcp.Dispose();
        señal.Set();
        cierreCompleto.Set();
    }
}

internal sealed class JugadorConectado
{
    internal readonly Monopoly.Administracion.Jugador Oficial;
    internal Sesion Sesion;
    internal bool EliminacionAvisada;
    internal string Token { get; } = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    internal string Id => Oficial.Id;
    internal string Nombre => Oficial.Nombre;

    
// Crea el objeto.
    internal JugadorConectado(Monopoly.Administracion.Jugador jugador)
    {
        Oficial = jugador;
    }
}
