using Monopoly.Administracion;
using Monopoly.Hardware;

namespace Monopoly.Integracion;

/// <summary>Resultados físicos validados. El coordinador consume cada resultado una sola vez.</summary>
public sealed class ProveedorDadosFisico : IProveedorDados
{
    private ResultadoDados pendiente;
    public bool Entregar(ResultadoDados resultado)
    {
        if (pendiente != null) return false;
        pendiente = resultado ?? throw new ArgumentNullException(nameof(resultado));
        return true;
    }

    public bool IntentarConsumirResultado(out ResultadoDados resultado)
    {
        resultado = pendiente;
        pendiente = null;
        return resultado != null;
    }

    public void Descartar() => pendiente = null;
}

/// <summary>Solo desarrollo: genera una entrada DADO que atraviesa el mismo analizador serial.</summary>
public sealed class ProveedorDadosSimulado : IProveedorDados
{
    private readonly Random azar;
    public ProveedorDadosSimulado(int? semilla = null) => azar = semilla.HasValue ? new Random(semilla.Value) : new Random();

    public bool IntentarConsumirResultado(out ResultadoDados resultado)
    {
        int uno = azar.Next(1, 7);
        int dos = azar.Next(1, 7);
        AnalizadorHardware.IntentarAnalizar($"DADO|{uno}|{dos}|{uno + dos}", out var evento);
        resultado = evento.ResultadoDados;
        return resultado != null;
    }
}

/// <summary>Observa lo consumido sin modificar ni repetir las reglas del coordinador.</summary>
internal sealed class ProveedorDadosObservado : IProveedorDados
{
    private readonly IProveedorDados origen;
    private readonly ProveedorDadosFisico serial;
    internal ResultadoDados Ultimo { get; private set; }
    internal ProveedorDadosObservado(IProveedorDados origen, ProveedorDadosFisico serial)
    {
        this.origen = origen;
        this.serial = serial;
    }

    public bool IntentarConsumirResultado(out ResultadoDados resultado)
    {
        bool disponible = serial.IntentarConsumirResultado(out resultado) || origen.IntentarConsumirResultado(out resultado);
        Ultimo = disponible ? resultado : null;
        return disponible;
    }

    internal void Limpiar() { Ultimo = null; serial.Descartar(); }
}
