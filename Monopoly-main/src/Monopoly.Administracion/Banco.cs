#nullable enable

using System.Globalization;
using System.Text;

namespace Monopoly.Administracion;





public sealed class ResultadoOperacion
{
    public bool FueExitosa { get; }
    public string Mensaje { get; }
    public decimal SaldoAnterior { get; }
    public decimal SaldoActual { get; }
    public bool FueRechazadaPorFondosInsuficientes { get; }

// Crea el objeto.
    private ResultadoOperacion(
        bool fueExitosa,
        string mensaje,
        decimal saldoAnterior,
        decimal saldoActual,
        bool fueRechazadaPorFondosInsuficientes)
    {
        FueExitosa = fueExitosa;
        Mensaje = mensaje;
        SaldoAnterior = saldoAnterior;
        SaldoActual = saldoActual;
        FueRechazadaPorFondosInsuficientes = fueRechazadaPorFondosInsuficientes;
    }

// Ejecuta Exito.
    public static ResultadoOperacion Exito(string mensaje, decimal saldoAnterior, decimal saldoActual)
        => new(true, mensaje, saldoAnterior, saldoActual, false);

// Ejecuta Error.
    public static ResultadoOperacion Error(
        string mensaje,
        decimal saldoActual = 0,
        bool fueRechazadaPorFondosInsuficientes = false)
        => new(false, mensaje, saldoActual, saldoActual, fueRechazadaPorFondosInsuficientes);
}





public sealed class Banco
{
    public const string IdentificadorBanco = "BANCO";

    private readonly RegistroJugadores _jugadores = new();
    private readonly int _maximoJugadores;
    private readonly object _bloqueo = new();

    
    public HistorialTransacciones Historial { get; } = new();

    
// Crea el objeto.
    public Banco(int maximoJugadores = 4)
    {
        if (maximoJugadores <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximoJugadores));

        _maximoJugadores = maximoJugadores;
    }

    
    public int CantidadJugadores
    {
        get
        {
            lock (_bloqueo)
                return _jugadores.Cantidad;
        }
    }

    
    public int CantidadJugadoresActivos
    {
        get
        {
            lock (_bloqueo)
            {
                int cantidad = 0;
                _jugadores.Recorrer(jugador =>
                {
                    if (jugador.EstaActivo)
                        cantidad++;
                });

                return cantidad;
            }
        }
    }

    
// Ejecuta RegistrarJugador.
    public ResultadoOperacion RegistrarJugador(string id, string nombre, decimal saldoInicial)
    {
        lock (_bloqueo)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(nombre) || saldoInicial < 0)
                return ResultadoOperacion.Error("Identificador, nombre y saldo inicial no son válidos.");

            id = id.Trim();
            nombre = nombre.Trim();
            if (id.Equals(IdentificadorBanco, StringComparison.OrdinalIgnoreCase))
                return ResultadoOperacion.Error("El identificador BANCO está reservado.");

            if (_jugadores.Cantidad >= _maximoJugadores)
                return ResultadoOperacion.Error("Ya se alcanzó el máximo de jugadores.");

            if (_jugadores.BuscarPorId(id) is not null)
                return ResultadoOperacion.Error("Ya existe un jugador con ese identificador.");

            var jugador = new Jugador(id, nombre, saldoInicial);
            _jugadores.Agregar(jugador);
            return ResultadoOperacion.Exito("Jugador registrado.", 0, jugador.Saldo);
        }
    }

    
// Ejecuta ConsultarJugador.
    public Jugador? ConsultarJugador(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        lock (_bloqueo)
            return _jugadores.BuscarPorId(id);
    }

    
// Ejecuta ConsultarSaldo.
    public decimal? ConsultarSaldo(string id)
    {
        return ConsultarJugador(id)?.Saldo;
    }

    
// Ejecuta RecorrerJugadores.
    public void RecorrerJugadores(Action<Jugador> accion)
    {
        ArgumentNullException.ThrowIfNull(accion);
        lock (_bloqueo)
            _jugadores.Recorrer(accion);
    }

// Ejecuta CancelarRegistroIncompleto.
    internal void CancelarRegistroIncompleto(string idJugador)
    {
        lock (_bloqueo)
            _jugadores.Retirar(idJugador);
    }

    
// Ejecuta GenerarResumenJugadores.
    public string GenerarResumenJugadores()
    {
        lock (_bloqueo)
        {
            var resumen = new StringBuilder();
            _jugadores.Recorrer(jugador =>
            {
                if (resumen.Length > 0)
                    resumen.Append('#');

                resumen.Append(jugador.Id)
                    .Append(';')
                    .Append(jugador.Nombre)
                    .Append(';')
                    .Append(jugador.Saldo.ToString(CultureInfo.InvariantCulture))
                    .Append(';')
                    .Append(jugador.PosicionActual)
                    .Append(';')
                    .Append(jugador.EstaActivo);
            });

            return resumen.ToString();
        }
    }

    


// Ejecuta Abonar.
    public ResultadoOperacion Abonar(
        string idJugador,
        decimal monto,
        string motivo,
        TipoTransaccion tipo,
        int numeroTurno)
    {
        lock (_bloqueo)
        {
            if (!EsMontoValido(monto))
                return ResultadoOperacion.Error("El monto debe ser mayor que cero.");

            if (numeroTurno < 0)
                return ResultadoOperacion.Error("El número de turno no puede ser negativo.");

            if (tipo is not TipoTransaccion.GananciaPorEvento and not TipoTransaccion.PremioPorPasarInicio)
                return ResultadoOperacion.Error("El tipo de transacción no corresponde a un abono del Banco.");

            if (string.IsNullOrWhiteSpace(motivo))
                return ResultadoOperacion.Error("La descripción de la operación es obligatoria.");

            Jugador? jugador = _jugadores.BuscarPorId(idJugador);
            if (jugador is null)
                return ResultadoOperacion.Error("El jugador no está registrado.");

            if (!jugador.EstaActivo)
                return ResultadoOperacion.Error("El jugador está eliminado.");

            decimal saldoAnterior = jugador.Saldo;
            jugador.EstablecerSaldoDesdeBanco(saldoAnterior + monto);
            Historial.Registrar(numeroTurno, tipo, IdentificadorBanco, jugador.Id, monto, motivo.Trim());
            return ResultadoOperacion.Exito($"Abono realizado: {motivo}", saldoAnterior, jugador.Saldo);
        }
    }

    


// Ejecuta Cobrar.
    public ResultadoOperacion Cobrar(
        string idJugador,
        decimal monto,
        string motivo,
        TipoTransaccion tipo,
        int numeroTurno)
    {
        lock (_bloqueo)
        {
            if (!EsMontoValido(monto))
                return ResultadoOperacion.Error("El monto debe ser mayor que cero.");

            if (numeroTurno < 0)
                return ResultadoOperacion.Error("El número de turno no puede ser negativo.");

            if (tipo is not TipoTransaccion.CompraPropiedad
                and not TipoTransaccion.PagoAlBanco
                and not TipoTransaccion.PerdidaPorEvento)
            {
                return ResultadoOperacion.Error("El tipo de transacción no corresponde a un cobro del Banco.");
            }

            if (string.IsNullOrWhiteSpace(motivo))
                return ResultadoOperacion.Error("La descripción de la operación es obligatoria.");

            Jugador? jugador = _jugadores.BuscarPorId(idJugador);
            if (jugador is null)
                return ResultadoOperacion.Error("El jugador no está registrado.");

            if (!jugador.EstaActivo)
                return ResultadoOperacion.Error("El jugador está eliminado.");

            if (jugador.Saldo < monto)
                return ResultadoOperacion.Error("El jugador no tiene saldo suficiente.", jugador.Saldo, true);

            decimal saldoAnterior = jugador.Saldo;
            jugador.EstablecerSaldoDesdeBanco(saldoAnterior - monto);
            Historial.Registrar(numeroTurno, tipo, jugador.Id, IdentificadorBanco, monto, motivo.Trim());
            return ResultadoOperacion.Exito($"Cobro realizado: {motivo}", saldoAnterior, jugador.Saldo);
        }
    }

    


// Ejecuta Transferir.
    public ResultadoOperacion Transferir(
        string idOrigen,
        string idDestino,
        decimal monto,
        string motivo,
        TipoTransaccion tipo,
        int numeroTurno)
    {
        lock (_bloqueo)
        {
            if (string.IsNullOrWhiteSpace(idOrigen) || string.IsNullOrWhiteSpace(idDestino))
                return ResultadoOperacion.Error("Los identificadores de origen y destino son obligatorios.");

            if (idOrigen.Equals(idDestino, StringComparison.OrdinalIgnoreCase))
                return ResultadoOperacion.Error("El origen y el destino deben ser jugadores distintos.");

            if (!EsMontoValido(monto))
                return ResultadoOperacion.Error("El monto debe ser mayor que cero.");

            if (numeroTurno < 0)
                return ResultadoOperacion.Error("El número de turno no puede ser negativo.");

            if (tipo is not TipoTransaccion.PagoAlquiler and not TipoTransaccion.PagoEntreJugadores)
                return ResultadoOperacion.Error("El tipo de transacción no corresponde a un pago entre jugadores.");

            if (string.IsNullOrWhiteSpace(motivo))
                return ResultadoOperacion.Error("La descripción de la operación es obligatoria.");

            Jugador? origen = _jugadores.BuscarPorId(idOrigen);
            Jugador? destino = _jugadores.BuscarPorId(idDestino);
            if (origen is null || destino is null)
                return ResultadoOperacion.Error("El jugador origen o destino no está registrado.");

            if (!origen.EstaActivo || !destino.EstaActivo)
                return ResultadoOperacion.Error("El origen y el destino deben seguir activos.");

            if (origen.Saldo < monto)
                return ResultadoOperacion.Error("El jugador origen no tiene saldo suficiente.", origen.Saldo, true);

            decimal saldoAnterior = origen.Saldo;
            origen.EstablecerSaldoDesdeBanco(saldoAnterior - monto);
            destino.EstablecerSaldoDesdeBanco(destino.Saldo + monto);
            Historial.Registrar(numeroTurno, tipo, origen.Id, destino.Id, monto, motivo.Trim());
            return ResultadoOperacion.Exito($"Transferencia realizada: {motivo}", saldoAnterior, origen.Saldo);
        }
    }

// Ejecuta EsMontoValido.
    private static bool EsMontoValido(decimal monto) => monto > 0;
}
