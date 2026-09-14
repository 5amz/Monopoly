namespace Monopoly.Administracion;

/// <summary>Registro inmutable de una operación económica validada por Banco.</summary>
public sealed class Transaccion
{
    public long Id { get; }
    public DateTime FechaHora { get; }
    public int NumeroTurno { get; }
    public TipoTransaccion Tipo { get; }
    public string IdJugadorOrigen { get; }
    public string IdJugadorDestino { get; }
    public decimal Monto { get; }
    public string Descripcion { get; }

    /// <summary>Crea una transacción ya validada y asignada por el historial.</summary>
    public Transaccion(
        long id,
        DateTime fechaHora,
        int numeroTurno,
        TipoTransaccion tipo,
        string idJugadorOrigen,
        string idJugadorDestino,
        decimal monto,
        string descripcion)
    {
        Id = id;
        FechaHora = fechaHora;
        NumeroTurno = numeroTurno;
        Tipo = tipo;
        IdJugadorOrigen = idJugadorOrigen;
        IdJugadorDestino = idJugadorDestino;
        Monto = monto;
        Descripcion = descripcion;
    }

    /// <summary>Convierte una transacción en una línea legible para pantalla o TXT.</summary>
    public string ConvertirALinea()
        => $"{Id}|{FechaHora:O}|Turno={NumeroTurno}|{Tipo}|Origen={IdJugadorOrigen}|Destino={IdJugadorDestino}|Monto={Monto}|{Descripcion}";
}
