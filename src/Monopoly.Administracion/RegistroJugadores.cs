#nullable enable

namespace Monopoly.Administracion;

// Registro lineal propio para no depender de List o LinkedList 
internal sealed class RegistroJugadores
{
    private NodoJugador? _primero;
    private int _cantidad;

    public int Cantidad => _cantidad;

    // Agrega un jugador al final si su ID aún no existe
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

    // Recorre los nodos y devuelve el jugador cuyo ID coincide
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

        // Crea un nodo que contiene un jugador
        public NodoJugador(Jugador jugador)
        {
            Jugador = jugador;
        }
    }
}
