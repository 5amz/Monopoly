using Monopoly.Administracion;
using Monopoly.Hardware;

namespace Monopoly.Integracion;


public sealed class ProveedorDadosFisico : IProveedorDados
{
    private ResultadoDados pendiente;
// Ejecuta Entregar.
    public bool Entregar(ResultadoDados resultado)
    {
        if (pendiente != null) return false;
        pendiente = resultado ?? throw new ArgumentNullException(nameof(resultado));
        return true;
    }

// Ejecuta IntentarConsumirResultado.
    public bool IntentarConsumirResultado(out ResultadoDados resultado)
    {
        resultado = pendiente;
        pendiente = null;
        return resultado != null;
    }

// Ejecuta Descartar.
    public void Descartar() => pendiente = null;
}


public sealed class ProveedorDadosSimulado : IProveedorDados
{
    private readonly Random azar;
// Crea el objeto.
    public ProveedorDadosSimulado(int? semilla = null) => azar = semilla.HasValue ? new Random(semilla.Value) : new Random();

// Ejecuta IntentarConsumirResultado.
    public bool IntentarConsumirResultado(out ResultadoDados resultado)
    {
        int uno = azar.Next(1, 7);
        int dos = azar.Next(1, 7);
        AnalizadorHardware.IntentarAnalizar($"DADO|{uno}|{dos}|{uno + dos}", out var evento);
        resultado = evento.ResultadoDados;
        return resultado != null;
    }
}


internal sealed class ProveedorDadosObservado : IProveedorDados
{
    private readonly IProveedorDados origen;
    private readonly ProveedorDadosFisico serial;
    internal ResultadoDados Ultimo { get; private set; }
// Crea el objeto.
    internal ProveedorDadosObservado(IProveedorDados origen, ProveedorDadosFisico serial)
    {
        this.origen = origen;
        this.serial = serial;
    }

// Ejecuta IntentarConsumirResultado.
    public bool IntentarConsumirResultado(out ResultadoDados resultado)
    {
        bool disponible = serial.IntentarConsumirResultado(out resultado) || origen.IntentarConsumirResultado(out resultado);
        Ultimo = disponible ? resultado : null;
        return disponible;
    }

// Ejecuta Limpiar.
    internal void Limpiar() { Ultimo = null; serial.Descartar(); }
}
