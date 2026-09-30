using Monopoly.Protocolo;

namespace Monopoly.Integracion;


public sealed class Servidor : IServidorEmbebido, IDisposable
{
    private readonly ServidorIntegrado oficial;
// Crea el objeto.
    public Servidor(ConfiguracionPartida configuracion = null) => oficial = new ServidorIntegrado(configuracion);
    public event Action ServidorListo { add => oficial.ServidorListo += value; remove => oficial.ServidorListo -= value; }
    public event Action<string> ServidorFallo { add => oficial.ServidorFallo += value; remove => oficial.ServidorFallo -= value; }
// Ejecuta Iniciar.
    public void Iniciar(int puerto) => oficial.Iniciar(puerto);
// Ejecuta Detener.
    public void Detener() => oficial.Detener();
// Ejecuta Dispose.
    public void Dispose() => oficial.Dispose();
}
