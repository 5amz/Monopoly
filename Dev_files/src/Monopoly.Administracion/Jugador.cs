#nullable enable

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

// Registro lineal propio para no depender de List o LinkedList.
// Se mantiene internal porque solamente Banco debe conocer esta estructura.
internal sealed class RegistroJugadores
{
    private NodoJugador? _primero;
    private int _cantidad;

    public int Cantidad => _cantidad;

    // Agrega un jugador al final si su ID aún no existe.
    public bool Agregar(Jugador jugador)
    {
        if (BuscarPorId(jugador.Id) is not null)
            return false;

        var nuevo = new NodoJugador(jugador);
        if (_primero is null)
        {
            _primero = nuevo;
        }
        else
        {
            NodoJugador actual = _primero;
            while (actual.Siguiente is not null)
                actual = actual.Siguiente;

            actual.Siguiente = nuevo;
        }

        _cantidad++;
        return true;
    }

    // Recorre los nodos y devuelve el jugador cuyo ID coincide.
    public Jugador? BuscarPorId(string id)
    {
        NodoJugador? actual = _primero;
        while (actual is not null)
        {
            if (actual.Jugador.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                return actual.Jugador;

            actual = actual.Siguiente;
        }

        return null;
    }

    // Recorre los jugadores sin entregar acceso a los nodos internos.
    public void Recorrer(Action<Jugador> accion)
    {
        if (accion is null)
            throw new ArgumentNullException(nameof(accion));

        NodoJugador? actual = _primero;
        while (actual is not null)
        {
            accion(actual.Jugador);
            actual = actual.Siguiente;
        }
    }

    private sealed class NodoJugador
    {
        public Jugador Jugador { get; }
        public NodoJugador? Siguiente { get; set; }

        public NodoJugador(Jugador jugador)
        {
            Jugador = jugador;
        }
    }
}
