#nullable enable

namespace Monopoly.Administracion;

/// <summary>
/// Autoridad para registrar jugadores, modificar dinero y guardar el historial
/// oficial de cada operación económica.
/// </summary>
public sealed class Banco
{
    public const string IdentificadorBanco = "BANCO";

    private readonly RegistroJugadores _jugadores = new();
    private readonly int _maximoJugadores;
    private readonly object _bloqueo = new();

    /// <summary>Historial oficial; sus nodos permanecen privados.</summary>
    public HistorialTransacciones Historial { get; } = new();

    /// <summary>Crea el Banco e indica cuántos jugadores puede registrar.</summary>
    public Banco(int maximoJugadores = 4)
    {
        if (maximoJugadores <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximoJugadores));

        _maximoJugadores = maximoJugadores;
    }

    /// <summary>Obtiene la cantidad actual de jugadores registrados.</summary>
    public int CantidadJugadores
    {
        get
        {
            lock (_bloqueo)
                return _jugadores.Cantidad;
        }
    }

    /// <summary>Valida y registra un jugador nuevo.</summary>
    public ResultadoOperacion RegistrarJugador(string id, string nombre, decimal saldoInicial)
    {
        lock (_bloqueo)
        {
            if (_jugadores.Cantidad >= _maximoJugadores)
                return ResultadoOperacion.Error("Ya se alcanzó el máximo de jugadores.");

            if (_jugadores.BuscarPorId(id) is not null)
                return ResultadoOperacion.Error("Ya existe un jugador con ese identificador.");

            var jugador = new Jugador(id, nombre, saldoInicial);
            _jugadores.Agregar(jugador);
            return ResultadoOperacion.Exito("Jugador registrado.", 0, jugador.Saldo);
        }
    }

    /// <summary>Busca un jugador por ID; devuelve null si no existe.</summary>
    public Jugador? ConsultarJugador(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        lock (_bloqueo)
            return _jugadores.BuscarPorId(id);
    }

    /// <summary>Consulta el saldo oficial de un jugador.</summary>
    public decimal? ConsultarSaldo(string id)
    {
        return ConsultarJugador(id)?.Saldo;
    }

    /// <summary>
    /// Abona dinero y registra una ganancia por evento o por pasar por inicio.
    /// </summary>
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

            decimal saldoAnterior = jugador.Saldo;
            jugador.EstablecerSaldoDesdeBanco(saldoAnterior + monto);
            Historial.Registrar(numeroTurno, tipo, IdentificadorBanco, jugador.Id, monto, motivo.Trim());
            return ResultadoOperacion.Exito($"Abono realizado: {motivo}", saldoAnterior, jugador.Saldo);
        }
    }

    /// <summary>
    /// Cobra dinero a un jugador y registra una compra, pago al Banco o pérdida por evento.
    /// </summary>
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

            if (jugador.Saldo < monto)
                return ResultadoOperacion.Error("El jugador no tiene saldo suficiente.", jugador.Saldo);

            decimal saldoAnterior = jugador.Saldo;
            jugador.EstablecerSaldoDesdeBanco(saldoAnterior - monto);
            Historial.Registrar(numeroTurno, tipo, jugador.Id, IdentificadorBanco, monto, motivo.Trim());
            return ResultadoOperacion.Exito($"Cobro realizado: {motivo}", saldoAnterior, jugador.Saldo);
        }
    }

    /// <summary>
    /// Transfiere dinero entre dos jugadores y registra alquiler o pago entre jugadores.
    /// </summary>
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

            if (origen.Saldo < monto)
                return ResultadoOperacion.Error("El jugador origen no tiene saldo suficiente.", origen.Saldo);

            decimal saldoAnterior = origen.Saldo;
            origen.EstablecerSaldoDesdeBanco(saldoAnterior - monto);
            destino.EstablecerSaldoDesdeBanco(destino.Saldo + monto);
            Historial.Registrar(numeroTurno, tipo, origen.Id, destino.Id, monto, motivo.Trim());
            return ResultadoOperacion.Exito($"Transferencia realizada: {motivo}", saldoAnterior, origen.Saldo);
        }
    }

    private static bool EsMontoValido(decimal monto) => monto > 0;
}
