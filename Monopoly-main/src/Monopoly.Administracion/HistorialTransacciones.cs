using System.Text;

namespace Monopoly.Administracion;


public enum TipoTransaccion
{
    CompraPropiedad,
    PagoAlquiler,
    PagoAlBanco,
    PagoEntreJugadores,
    GananciaPorEvento,
    PerdidaPorEvento,
    PremioPorPasarInicio
}


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

    
// Crea el objeto.
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

    
// Ejecuta ConvertirALinea.
    public string ConvertirALinea()
        => $"{Id}|{FechaHora:O}|Turno={NumeroTurno}|{Tipo}|Origen={IdJugadorOrigen}|Destino={IdJugadorDestino}|Monto={Monto}|{Descripcion}";
}





public sealed class HistorialTransacciones
{
    private NodoTransaccion _primero;
    private NodoTransaccion _ultimo;
    private long _siguienteId = 1;

    public int Cantidad { get; private set; }

    
// Ejecuta Registrar.
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

    
// Ejecuta RecorrerAntiguaAReciente.
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

    
// Ejecuta RecorrerRecienteAAntigua.
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

    
// Ejecuta GenerarReporteCompleto.
    public string GenerarReporteCompleto()
    {
        return GenerarReporte(t => true, desdeAntigua: true);
    }

    
// Ejecuta GenerarReporteInverso.
    public string GenerarReporteInverso()
    {
        return GenerarReporte(t => true, desdeAntigua: false);
    }

    
// Ejecuta GenerarReportePorJugador.
    public string GenerarReportePorJugador(string idJugador)
    {
        if (string.IsNullOrWhiteSpace(idJugador))
            return string.Empty;

        return GenerarReporte(
            t => t.IdJugadorOrigen.Equals(idJugador, StringComparison.OrdinalIgnoreCase)
                || t.IdJugadorDestino.Equals(idJugador, StringComparison.OrdinalIgnoreCase),
            desdeAntigua: true);
    }

    
// Ejecuta GenerarReportePorTipo.
    public string GenerarReportePorTipo(TipoTransaccion tipo)
    {
        return GenerarReporte(t => t.Tipo == tipo, desdeAntigua: true);
    }

    
// Ejecuta ExportarATxt.
    public void ExportarATxt(string rutaArchivo)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(rutaArchivo));

        string directorio = Path.GetDirectoryName(Path.GetFullPath(rutaArchivo));
        if (!string.IsNullOrEmpty(directorio))
            Directory.CreateDirectory(directorio);
        File.WriteAllText(rutaArchivo, GenerarReporteCompleto(), Encoding.UTF8);
    }

// Ejecuta GenerarReporte.
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

// Crea el objeto.
        public NodoTransaccion(Transaccion transaccion)
        {
            Transaccion = transaccion;
        }
    }
}
