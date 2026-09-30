#nullable enable

namespace Monopoly.Administracion;

// Representa la información oficial de un participante
// El saldo solo puede cambiarse desde Banco

public sealed class Jugador
{
    public string Id { get; }
    public string Nombre { get; }
    public decimal Saldo { get; private set; }

    // El coordinador sincroniza estos datos con el tablero oficial.
    public int PosicionActual { get; private set; }
    public bool EstaActivo { get; private set; }

    // Índice lineal propio. Los objetos y su propietario son autoritativos en Tablero.
    public PropiedadesJugador Propiedades { get; } = new();
    public int CantidadPropiedades => Propiedades.Cantidad;
    public decimal Patrimonio => Saldo + Propiedades.ValorOficial;

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
    }

    // Actualiza el saldo desde el Banco; no es accesible al cliente
    internal void EstablecerSaldoDesdeBanco(decimal nuevoSaldo)
    {
        Saldo = nuevoSaldo;
    }

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

}

/// <summary>
/// Lista simplemente enlazada creada por el equipo. Guarda referencias a las
/// propiedades oficiales; nunca copia precios ni propietarios del tablero.
/// </summary>
public sealed class PropiedadesJugador
{
    private NodoPropiedad? _primero;
    private NodoPropiedad? _ultimo;
    public int Cantidad { get; private set; }
    public decimal ValorOficial
    {
        get
        {
            decimal valor = 0;
            Recorrer(propiedad => valor += propiedad.Precio);
            return valor;
        }
    }

    public bool Contiene(int idPropiedad)
    {
        for (NodoPropiedad? nodo = _primero; nodo is not null; nodo = nodo.Siguiente)
            if (nodo.Propiedad.Id == idPropiedad)
                return true;
        return false;
    }

    public void Recorrer(Action<global::Monopoly.Propiedad> accion)
    {
        ArgumentNullException.ThrowIfNull(accion);
        for (NodoPropiedad? nodo = _primero; nodo is not null; nodo = nodo.Siguiente)
            accion(nodo.Propiedad);
    }

    internal void AgregarDesdeTablero(global::Monopoly.Propiedad propiedad)
    {
        if (Contiene(propiedad.Id))
            return;
        var nodo = new NodoPropiedad(propiedad);
        if (_ultimo is null)
            _primero = nodo;
        else
            _ultimo.Siguiente = nodo;
        _ultimo = nodo;
        Cantidad++;
    }

    internal void VaciarDesdeTablero()
    {
        _primero = null;
        _ultimo = null;
        Cantidad = 0;
    }

    private sealed class NodoPropiedad
    {
        public global::Monopoly.Propiedad Propiedad { get; }
        public NodoPropiedad? Siguiente { get; set; }
        public NodoPropiedad(global::Monopoly.Propiedad propiedad) => Propiedad = propiedad;
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

    // Solo se usa para deshacer un registro antes de que la partida comience.
    public bool Retirar(string id)
    {
        NodoJugador? anterior = null;
        NodoJugador? actual = _primero;
        while (actual is not null)
        {
            if (actual.Jugador.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            {
                if (anterior is null) _primero = actual.Siguiente;
                else anterior.Siguiente = actual.Siguiente;
                _cantidad--;
                return true;
            }
            anterior = actual;
            actual = actual.Siguiente;
        }
        return false;
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
