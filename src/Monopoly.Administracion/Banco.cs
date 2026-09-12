namespace Monopoly.Administracion;

/// Autoridad para registrar jugadores y modificar dinero oficial
/// Ningún cliente debe cambiar el saldo de Jugador directamente
public sealed class Banco
{
    private readonly RegistroJugadores _jugadores = new();
    private readonly int _maximoJugadores;

    ///Crea el Banco e indica cuántos jugadores puede registrar
    public Banco(int maximoJugadores = 4)
    {
        if (maximoJugadores <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximoJugadores));

        _maximoJugadores = maximoJugadores;
    }

    public int CantidadJugadores => _jugadores.Cantidad;

    ///Valida y registra un jugador nuevo
    public ResultadoOperacion RegistrarJugador(string id, string nombre, decimal saldoInicial)
    {
        if (CantidadJugadores >= _maximoJugadores)
            return ResultadoOperacion.Error("Ya se alcanzó el máximo de jugadores.");

        if (_jugadores.BuscarPorId(id) is not null)
            return ResultadoOperacion.Error("Ya existe un jugador con ese identificador.");

        var jugador = new Jugador(id, nombre, saldoInicial);
        _jugadores.Agregar(jugador);
        return ResultadoOperacion.Exito("Jugador registrado.", 0, jugador.Saldo);
    }

    ///Busca un jugador por ID, devuelve null si no existe
    public Jugador? ConsultarJugador(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return _jugadores.BuscarPorId(id);
    }

    ///Consulta el saldo oficial de un jugador
    public decimal? ConsultarSaldo(string id)
    {
        return ConsultarJugador(id)?.Saldo;
    }

    ///Agrega dinero a un jugador después de validar la operación
    public ResultadoOperacion Abonar(string idJugador, decimal monto, string motivo)
    {
        if (!EsMontoValido(monto))
            return ResultadoOperacion.Error("El monto debe ser mayor que cero.");

        Jugador? jugador = ConsultarJugador(idJugador);
        if (jugador is null)
            return ResultadoOperacion.Error("El jugador no está registrado.");

        decimal saldoAnterior = jugador.Saldo;
        jugador.EstablecerSaldoDesdeBanco(saldoAnterior + monto);
        return ResultadoOperacion.Exito($"Abono realizado: {motivo}", saldoAnterior, jugador.Saldo);
    }

    ///Descuenta dinero si el jugador existe y tiene fondos suficientes
    public ResultadoOperacion Cobrar(string idJugador, decimal monto, string motivo)
    {
        if (!EsMontoValido(monto))
            return ResultadoOperacion.Error("El monto debe ser mayor que cero.");

        Jugador? jugador = ConsultarJugador(idJugador);
        if (jugador is null)
            return ResultadoOperacion.Error("El jugador no está registrado.");

        if (jugador.Saldo < monto)
            return ResultadoOperacion.Error("El jugador no tiene saldo suficiente.", jugador.Saldo);

        decimal saldoAnterior = jugador.Saldo;
        jugador.EstablecerSaldoDesdeBanco(saldoAnterior - monto);
        return ResultadoOperacion.Exito($"Cobro realizado: {motivo}", saldoAnterior, jugador.Saldo);
    }

    ///Transfiere dinero entre dos jugadores registrados
    public ResultadoOperacion Transferir(string idOrigen, string idDestino, decimal monto, string motivo)
    {
        if (idOrigen.Equals(idDestino, StringComparison.OrdinalIgnoreCase))
            return ResultadoOperacion.Error("El origen y el destino deben ser jugadores distintos.");

        if (!EsMontoValido(monto))
            return ResultadoOperacion.Error("El monto debe ser mayor que cero.");

        Jugador? origen = ConsultarJugador(idOrigen);
        Jugador? destino = ConsultarJugador(idDestino);
        if (origen is null || destino is null)
            return ResultadoOperacion.Error("El jugador origen o destino no está registrado.");

        if (origen.Saldo < monto)
            return ResultadoOperacion.Error("El jugador origen no tiene saldo suficiente.", origen.Saldo);

        origen.EstablecerSaldoDesdeBanco(origen.Saldo - monto);
        destino.EstablecerSaldoDesdeBanco(destino.Saldo + monto);
        return ResultadoOperacion.Exito($"Transferencia realizada: {motivo}", origen.Saldo + monto, origen.Saldo);
    }

    ///Indica si un monto puede utilizarse en una operación
    private static bool EsMontoValido(decimal monto) => monto > 0;
}
