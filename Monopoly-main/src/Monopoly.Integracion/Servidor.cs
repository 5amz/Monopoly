using Monopoly.Protocolo;

namespace Monopoly.Integracion;

/// <summary>Nombre exigido por la rúbrica; delega al único servidor de producción.</summary>
public sealed class Servidor : IServidorEmbebido, IDisposable
{
    private readonly ServidorIntegrado oficial;
    public Servidor(ConfiguracionPartida configuracion = null) => oficial = new ServidorIntegrado(configuracion);
    public event Action ServidorListo { add => oficial.ServidorListo += value; remove => oficial.ServidorListo -= value; }
    public event Action<string> ServidorFallo { add => oficial.ServidorFallo += value; remove => oficial.ServidorFallo -= value; }
    public void Iniciar(int puerto) => oficial.Iniciar(puerto);
    public void Detener() => oficial.Detener();
    public void Dispose() => oficial.Dispose();
}
