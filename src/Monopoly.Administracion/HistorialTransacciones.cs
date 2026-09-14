using System.Text;

namespace Monopoly.Administracion;

/// <summary>
/// Lista doblemente enlazada propia para conservar las transacciones oficiales.
/// Sus nodos se mantienen privados para que otros módulos no los manipulen.
/// </summary>
public sealed class HistorialTransacciones
{
    private NodoTransaccion _primero;
    private NodoTransaccion _ultimo;
    private long _siguienteId = 1;

    public int Cantidad { get; private set; }

    /// <summary>Crea y agrega una transacción al final del historial.</summary>
    public Transaccion Registrar(
        int numeroTurno,
        TipoTransaccion tipo,
        string idJugadorOrigen,
        string idJugadorDestino,
        decimal monto,
        string descripcion)
    {
        var transaccion = new Transaccion(
            _siguienteId++,
            DateTime.Now,
            numeroTurno,
            tipo,
            idJugadorOrigen,
            idJugadorDestino,
            monto,
            descripcion);

        var nuevo = new NodoTransaccion(transaccion);
        if (_primero is null)
        {
            _primero = nuevo;
            _ultimo = nuevo;
        }
        else
        {
            nuevo.Anterior = _ultimo;
            _ultimo.Siguiente = nuevo;
            _ultimo = nuevo;
        }

        Cantidad++;
        return transaccion;
    }

    /// <summary>Recorre las transacciones desde la más antigua a la más reciente.</summary>
    public void RecorrerAntiguaAReciente(Action<Transaccion> accion)
    {
        if (accion is null)
            throw new ArgumentNullException(nameof(accion));

        NodoTransaccion actual = _primero;
        while (actual is not null)
        {
            accion(actual.Transaccion);
            actual = actual.Siguiente;
        }
    }

    /// <summary>Recorre las transacciones desde la más reciente a la más antigua.</summary>
    public void RecorrerRecienteAAntigua(Action<Transaccion> accion)
    {
        if (accion is null)
            throw new ArgumentNullException(nameof(accion));

        NodoTransaccion actual = _ultimo;
        while (actual is not null)
        {
            accion(actual.Transaccion);
            actual = actual.Anterior;
        }
    }

    /// <summary>Genera el historial completo en orden cronológico.</summary>
    public string GenerarReporteCompleto()
    {
        return GenerarReporte(t => true, desdeAntigua: true);
    }

    /// <summary>Genera el historial completo en orden inverso.</summary>
    public string GenerarReporteInverso()
    {
        return GenerarReporte(t => true, desdeAntigua: false);
    }

    /// <summary>Busca todas las transacciones donde el jugador sea origen o destino.</summary>
    public string GenerarReportePorJugador(string idJugador)
    {
        if (string.IsNullOrWhiteSpace(idJugador))
            return string.Empty;

        return GenerarReporte(
            t => t.IdJugadorOrigen.Equals(idJugador, StringComparison.OrdinalIgnoreCase)
                || t.IdJugadorDestino.Equals(idJugador, StringComparison.OrdinalIgnoreCase),
            desdeAntigua: true);
    }

    /// <summary>Busca todas las transacciones de un tipo.</summary>
    public string GenerarReportePorTipo(TipoTransaccion tipo)
    {
        return GenerarReporte(t => t.Tipo == tipo, desdeAntigua: true);
    }

    /// <summary>Escribe una copia legible del historial en un archivo TXT.</summary>
    public void ExportarATxt(string rutaArchivo)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(rutaArchivo));

        File.WriteAllText(rutaArchivo, GenerarReporteCompleto(), Encoding.UTF8);
    }

    private string GenerarReporte(Func<Transaccion, bool> filtro, bool desdeAntigua)
    {
        var texto = new StringBuilder();
        Action<Transaccion> agregarSiCoincide = transaccion =>
        {
            if (filtro(transaccion))
                texto.AppendLine(transaccion.ConvertirALinea());
        };

        if (desdeAntigua)
            RecorrerAntiguaAReciente(agregarSiCoincide);
        else
            RecorrerRecienteAAntigua(agregarSiCoincide);

        return texto.ToString();
    }

    private sealed class NodoTransaccion
    {
        public Transaccion Transaccion { get; }
        public NodoTransaccion Anterior { get; set; }
        public NodoTransaccion Siguiente { get; set; }

        public NodoTransaccion(Transaccion transaccion)
        {
            Transaccion = transaccion;
        }
    }
}
