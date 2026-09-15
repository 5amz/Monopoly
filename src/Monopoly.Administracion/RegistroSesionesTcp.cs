using System.Net.Sockets;

namespace Monopoly.Administracion;

/// <summary>
/// Registro lineal propio de sesiones TCP activas. Solo ServidorTcp lo utiliza.
/// </summary>
internal sealed class RegistroSesionesTcp
{
    private NodoSesion _primero;
    private readonly object _bloqueo = new();

    /// <summary>
    /// Registra una sesión para un jugador y devuelve una sesión anterior del
    /// mismo jugador para cerrarla durante una reconexión.
    /// </summary>
    public SesionClienteTcp Registrar(SesionClienteTcp sesion)
    {
        lock (_bloqueo)
        {
            NodoSesion actual = _primero;
            while (actual is not null)
            {
                if (actual.Sesion.IdJugador.Equals(sesion.IdJugador, StringComparison.OrdinalIgnoreCase))
                {
                    SesionClienteTcp anterior = actual.Sesion;
                    actual.Sesion = sesion;
                    return anterior;
                }

                actual = actual.Siguiente;
            }

            var nuevo = new NodoSesion(sesion);
            if (_primero is null)
            {
                _primero = nuevo;
            }
            else
            {
                actual = _primero;
                while (actual.Siguiente is not null)
                    actual = actual.Siguiente;

                actual.Siguiente = nuevo;
            }

            return null;
        }
    }

    /// <summary>Elimina exactamente la sesión indicada al terminar una conexión.</summary>
    public void Eliminar(SesionClienteTcp sesion)
    {
        lock (_bloqueo)
        {
            NodoSesion anterior = null;
            NodoSesion actual = _primero;

            while (actual is not null)
            {
                if (ReferenceEquals(actual.Sesion, sesion))
                {
                    if (anterior is null)
                        _primero = actual.Siguiente;
                    else
                        anterior.Siguiente = actual.Siguiente;

                    return;
                }

                anterior = actual;
                actual = actual.Siguiente;
            }
        }
    }

    /// <summary>Envía una notificación a cada sesión activa.</summary>
    public async Task NotificarATodosAsync(string linea)
    {
        SesionClienteTcp[] sesiones = CrearCopia();
        foreach (SesionClienteTcp sesion in sesiones)
            await sesion.IntentarEnviarAsync(linea);
    }

    /// <summary>Cierra las conexiones activas al detener el servidor.</summary>
    public void CerrarTodo()
    {
        SesionClienteTcp[] sesiones = CrearCopia();
        foreach (SesionClienteTcp sesion in sesiones)
            sesion.Cerrar();
    }

    private SesionClienteTcp[] CrearCopia()
    {
        lock (_bloqueo)
        {
            int cantidad = 0;
            NodoSesion actual = _primero;
            while (actual is not null)
            {
                cantidad++;
                actual = actual.Siguiente;
            }

            var sesiones = new SesionClienteTcp[cantidad];
            actual = _primero;
            int indice = 0;
            while (actual is not null)
            {
                sesiones[indice++] = actual.Sesion;
                actual = actual.Siguiente;
            }

            return sesiones;
        }
    }

    private sealed class NodoSesion
    {
        public SesionClienteTcp Sesion { get; set; }
        public NodoSesion Siguiente { get; set; }

        public NodoSesion(SesionClienteTcp sesion)
        {
            Sesion = sesion;
        }
    }
}

/// <summary>Encapsula una conexión identificada y serializa sus escrituras.</summary>
internal sealed class SesionClienteTcp
{
    private readonly TcpClient _cliente;
    private readonly StreamWriter _escritor;
    private readonly SemaphoreSlim _bloqueoEscritura = new(1, 1);

    public string IdJugador { get; }

    public SesionClienteTcp(string idJugador, TcpClient cliente, StreamWriter escritor)
    {
        IdJugador = idJugador;
        _cliente = cliente;
        _escritor = escritor;
    }

    /// <summary>Intenta enviar una línea sin detener el servidor si una sesión falla.</summary>
    public async Task IntentarEnviarAsync(string linea)
    {
        bool bloqueoAdquirido = false;
        try
        {
            await _bloqueoEscritura.WaitAsync();
            bloqueoAdquirido = true;
            await _escritor.WriteLineAsync(linea);
        }
        catch (IOException)
        {
            // La sesión se cerrará al terminar su ciclo de lectura.
        }
        catch (ObjectDisposedException)
        {
            // La conexión ya fue cerrada.
        }
        finally
        {
            if (bloqueoAdquirido)
                _bloqueoEscritura.Release();
        }
    }

    /// <summary>Cierra la conexión cuando el mismo jugador se reconecta o el servidor se detiene.</summary>
    public void Cerrar()
    {
        _cliente.Close();
    }
}
