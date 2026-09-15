using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Monopoly.Administracion;

/// <summary>
/// Servidor TCP de líneas de texto. Acepta clientes, conserva las sesiones
/// activas y delega toda regla de Monopoly a ServidorJuego y sus módulos.
/// </summary>
public sealed class ServidorTcp : IDisposable
{
    private readonly ServidorJuego _juego;
    private readonly int _puerto;
    private readonly object _bloqueoSolicitudes = new();
    private readonly RegistroSesionesTcp _sesiones = new();
    private TcpListener _escuchador;
    private CancellationTokenSource _cancelacion;

    public bool EstaEnEjecucion => _escuchador is not null;
    public int Puerto => _puerto;

    public ServidorTcp(ServidorJuego juego, int puerto = 5000)
    {
        _juego = juego ?? throw new ArgumentNullException(nameof(juego));
        if (puerto is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(puerto));

        _puerto = puerto;
    }

    /// <summary>Inicia la escucha TCP en todas las interfaces de red disponibles.</summary>
    public Task IniciarAsync()
    {
        if (EstaEnEjecucion)
            throw new InvalidOperationException("El servidor TCP ya está iniciado.");

        _cancelacion = new CancellationTokenSource();
        _escuchador = new TcpListener(IPAddress.Any, _puerto);
        _escuchador.Start();
        _ = AceptarClientesAsync(_cancelacion.Token);
        return Task.CompletedTask;
    }

    /// <summary>Detiene la escucha y cierra las sesiones TCP activas.</summary>
    public Task DetenerAsync()
    {
        if (!EstaEnEjecucion)
            return Task.CompletedTask;

        _cancelacion.Cancel();
        _escuchador.Stop();
        _sesiones.CerrarTodo();
        _cancelacion.Dispose();
        _cancelacion = null;
        _escuchador = null;
        return Task.CompletedTask;
    }

    private async Task AceptarClientesAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient cliente = await _escuchador.AcceptTcpClientAsync(token);
                _ = AtenderClienteAsync(cliente, token);
            }
        }
        catch (OperationCanceledException)
        {
            // DetenerAsync cancela la espera normalmente.
        }
        catch (ObjectDisposedException)
        {
            // El listener puede cerrarse mientras espera una conexión.
        }
    }

    private async Task AtenderClienteAsync(TcpClient cliente, CancellationToken token)
    {
        using (cliente)
        using (NetworkStream flujo = cliente.GetStream())
        using (var lector = new StreamReader(flujo, new UTF8Encoding(false), false, 1024, true))
        using (var escritor = new StreamWriter(flujo, new UTF8Encoding(false), 1024, true) { AutoFlush = true })
        {
            string idJugador = null;
            SesionClienteTcp sesion = null;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    string linea = await lector.ReadLineAsync(token);
                    if (linea is null)
                        break;

                    RespuestaProtocolo respuesta = ProcesarLinea(linea, ref idJugador);

                    if (respuesta.FueExitosa && sesion is null && idJugador is not null)
                    {
                        sesion = new SesionClienteTcp(idJugador, cliente, escritor);
                        SesionClienteTcp sesionAnterior = _sesiones.Registrar(sesion);
                        sesionAnterior?.Cerrar();
                    }

                    if (sesion is null)
                        await escritor.WriteLineAsync(respuesta.ConvertirALinea());
                    else
                        await sesion.IntentarEnviarAsync(respuesta.ConvertirALinea());

                    if (DebeNotificarActualizacion(respuesta))
                        await NotificarEstadoActualizadoAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // El servidor se está deteniendo.
            }
            catch (IOException)
            {
                // El cliente se desconectó mientras se leía o respondía.
            }
            catch (SocketException)
            {
                // Error de red local: la sesión se cierra sin afectar al juego.
            }
            finally
            {
                if (sesion is not null)
                    _sesiones.Eliminar(sesion);
            }
        }
    }

    private RespuestaProtocolo ProcesarLinea(string linea, ref string idJugador)
    {
        // Dos solicitudes no se procesan en paralelo: así el estado oficial
        // se observa y modifica en un orden definido por el servidor.
        lock (_bloqueoSolicitudes)
        {
            if (linea.Length > 512)
                return RespuestaProtocolo.Error("FORMATO_INVALIDO", "La solicitud supera el máximo de 512 caracteres.");

            if (!AnalizadorProtocolo.IntentarAnalizar(linea, out SolicitudProtocolo solicitud, out RespuestaProtocolo error))
                return error;

            if (solicitud.Comando == ComandoProtocolo.CONECTAR)
            {
                if (idJugador is not null)
                    return RespuestaProtocolo.Error("YA_IDENTIFICADO", "Esta conexión ya está identificada.");

                RespuestaProtocolo respuestaConexion = _juego.ConectarJugador(solicitud);
                if (respuestaConexion.FueExitosa)
                    idJugador = solicitud.IdJugador;

                return respuestaConexion;
            }

            if (idJugador is null)
                return RespuestaProtocolo.Error("NO_IDENTIFICADO", "Debe enviar CONECTAR antes de solicitar acciones.");

            return _juego.ProcesarSolicitud(idJugador, solicitud);
        }
    }

    private static bool DebeNotificarActualizacion(RespuestaProtocolo respuesta)
    {
        if (!respuesta.FueExitosa)
            return false;

        return respuesta.Codigo is nameof(ComandoProtocolo.CONECTAR)
            or nameof(ComandoProtocolo.TIRAR_DADOS)
            or nameof(ComandoProtocolo.COMPRAR_PROPIEDAD)
            or nameof(ComandoProtocolo.NO_COMPRAR)
            or nameof(ComandoProtocolo.TERMINAR_TURNO);
    }

    private Task NotificarEstadoActualizadoAsync()
    {
        string evento = RespuestaProtocolo.CrearEvento("ESTADO_ACTUALIZADO", _juego.GenerarResumenEstado());
        return _sesiones.NotificarATodosAsync(evento);
    }

    /// <summary>Libera los recursos de red si el servidor sigue activo.</summary>
    public void Dispose()
    {
        DetenerAsync().GetAwaiter().GetResult();
    }
}
