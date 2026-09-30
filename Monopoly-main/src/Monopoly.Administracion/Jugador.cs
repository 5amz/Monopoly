#nullable enable

namespace Monopoly.Administracion;




public sealed class Jugador
{
    public string Id { get; }
    public string Nombre { get; }
    public decimal Saldo { get; private set; }

    
    public int PosicionActual { get; private set; }
    public bool EstaActivo { get; private set; }

    
    public PropiedadesJugador Propiedades { get; } = new();
    public int CantidadPropiedades => Propiedades.Cantidad;
    public decimal Patrimonio => Saldo + Propiedades.ValorOficial;

    
// Crea el objeto.
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

    
// Ejecuta EstablecerSaldoDesdeBanco.
    internal void EstablecerSaldoDesdeBanco(decimal nuevoSaldo)
    {
        Saldo = nuevoSaldo;
    }

    
// Ejecuta ActualizarPosicionDesdeServidor.
    internal void ActualizarPosicionDesdeServidor(int nuevaPosicion)
    {
        PosicionActual = nuevaPosicion;
    }

    
// Ejecuta CambiarEstadoActivoDesdeServidor.
    internal void CambiarEstadoActivoDesdeServidor(bool activo)
    {
        EstaActivo = activo;
    }

}





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

// Ejecuta Contiene.
    public bool Contiene(int idPropiedad)
    {
        for (NodoPropiedad? nodo = _primero; nodo is not null; nodo = nodo.Siguiente)
            if (nodo.Propiedad.Id == idPropiedad)
                return true;
        return false;
    }

// Ejecuta Recorrer.
    public void Recorrer(Action<global::Monopoly.Propiedad> accion)
    {
        ArgumentNullException.ThrowIfNull(accion);
        for (NodoPropiedad? nodo = _primero; nodo is not null; nodo = nodo.Siguiente)
            accion(nodo.Propiedad);
    }

// Ejecuta AgregarDesdeTablero.
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

// Ejecuta VaciarDesdeTablero.
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
// Crea el objeto.
        public NodoPropiedad(global::Monopoly.Propiedad propiedad) => Propiedad = propiedad;
    }
}



internal sealed class RegistroJugadores
{
    private NodoJugador? _primero;
    private int _cantidad;

    public int Cantidad => _cantidad;

    
// Ejecuta Agregar.
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

    
// Ejecuta BuscarPorId.
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

    
// Ejecuta Retirar.
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

    
// Ejecuta Recorrer.
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

// Crea el objeto.
        public NodoJugador(Jugador jugador)
        {
            Jugador = jugador;
        }
    }
}
