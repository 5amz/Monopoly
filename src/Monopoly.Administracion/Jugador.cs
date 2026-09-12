namespace Monopoly.Administracion;

// Representa la información oficial de un participante
// El saldo solo puede cambiarse desde Banco

public sealed class Jugador
{
    public string Id { get; }
    public string Nombre { get; }
    public decimal Saldo { get; private set; }

    // Estos datos permiten integrar posteriormente tablero y turnos
    // El tablero decidirá cómo interpretar la posición
    public int PosicionActual { get; private set; }
    public bool EstaActivo { get; private set; }

    // La colección real de propiedades será responsabilidad del módulo correspondiente. Se deja el dato mínimo para mostrar estado
    public int CantidadPropiedades { get; private set; }

    // Crea un jugador y valida sus datos iniciales
    public Jugador(string id, string nombre, decimal saldoInicial)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("El identificador del jugador es obligatorio.", nameof(id));

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del jugador es obligatorio.", nameof(nombre));

        if (saldoInicial < 0)
            throw new ArgumentOutOfRangeException(nameof(saldoInicial), "El saldo inicial no puede ser negativo.");

        Id = id.Trim();
        Nombre = nombre.Trim();
        Saldo = saldoInicial;
        PosicionActual = 0;
        EstaActivo = true;
        CantidadPropiedades = 0;
    }

    // Actualiza el saldo desde el Banco; no es accesible al cliente
    internal void EstablecerSaldoDesdeBanco(decimal nuevoSaldo)
    {
        Saldo = nuevoSaldo;
    }

    // El futuro módulo de tablero debe solicitar el movimiento al servidor, no modifica esta propiedad desde un cliente.
    // Actualiza la posición como parte de una acción autorizada
    internal void ActualizarPosicionDesdeServidor(int nuevaPosicion)
    {
        PosicionActual = nuevaPosicion;
    }

    // Cambia el estado activo del jugador desde el servidor
    internal void CambiarEstadoActivoDesdeServidor(bool activo)
    {
        EstaActivo = activo;
    }

    // Actualiza el contador de propiedades recibido del módulo correspondiente
    internal void ActualizarCantidadPropiedadesDesdeServidor(int cantidad)
    {
        if (cantidad < 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad));

        CantidadPropiedades = cantidad;
    }
}
