using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;
public sealed class AnfitrionServidor
{
    private readonly IServidorEmbebido servidor;
    public bool Activo { get; private set; }

    public event Action? ServidorListo;
    public event Action<string>? ServidorFallo;
    
// Crea el objeto.
    public AnfitrionServidor(IServidorEmbebido servidor)
    {
        this.servidor = servidor;
        servidor.ServidorListo += () =>
        {
            Activo = true;
            ServidorListo?.Invoke();
        };
        servidor.ServidorFallo += texto =>
        {
            Activo = false;
            ServidorFallo?.Invoke(texto);
        };
    }

    
// Ejecuta Iniciar.
    public void Iniciar(int puerto)
    {
        _ = Task.Run(() =>
        {
            try
            {
                servidor.Iniciar(puerto);
            }
            catch (Exception e)when (e is SocketException or InvalidOperationException or ArgumentException)
            {
                Activo = false;
                ServidorFallo?.Invoke("No se pudo iniciar el servidor: " + e.Message);
            }
        });
    }

    
// Ejecuta Detener.
    public void Detener()
    {
        servidor.Detener();
        Activo = false;
    }
}
