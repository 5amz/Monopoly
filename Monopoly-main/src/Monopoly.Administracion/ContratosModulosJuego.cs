namespace Monopoly.Administracion;


public interface IValidadorTurnos
{
    
// Ejecuta EsTurnoActual.
    bool EsTurnoActual(string idJugador);
}





public interface IProveedorDados
{
// Ejecuta IntentarConsumirResultado.
    bool IntentarConsumirResultado(out ResultadoDados resultado);
}


public sealed class ResultadoDados
{
    public int Total { get; }
    public int? Dado1 { get; }
    public int? Dado2 { get; }
    public bool CarasDisponibles => Dado1.HasValue && Dado2.HasValue;

// Crea el objeto.
    public ResultadoDados(int total)
    {
        if (total is < 2 or > 12)
            throw new ArgumentOutOfRangeException(nameof(total));

        Total = total;
    }

// Crea el objeto.
    public ResultadoDados(int dado1, int dado2) : this(dado1, dado2, dado1 + dado2) { }

// Crea el objeto.
    public ResultadoDados(int dado1, int dado2, int total) : this(total)
    {
        Dado primero = new(dado1);
        Dado segundo = new(dado2);
        if (total != dado1 + dado2)
            throw new ArgumentException("El total debe coincidir con la suma de las caras.", nameof(total));

        Dado1 = primero.Valor;
        Dado2 = segundo.Valor;
    }
}


public interface IAccionesJuego
{
// Ejecuta TirarDados.
    ResultadoAccionJuego TirarDados(string idJugador);
// Ejecuta ComprarPropiedad.
    ResultadoAccionJuego ComprarPropiedad(string idJugador);
// Ejecuta NoComprarPropiedad.
    ResultadoAccionJuego NoComprarPropiedad(string idJugador);
// Ejecuta TerminarTurno.
    ResultadoAccionJuego TerminarTurno(string idJugador);
}


public interface IRegistroJugadoresJuego
{
// Ejecuta RegistrarJugadorEnJuego.
    ResultadoAccionJuego RegistrarJugadorEnJuego(string idJugador, string nombre);
}


public interface IEliminacionJugadoresJuego
{
// Ejecuta EliminarJugadorDelJuego.
    ResultadoAccionJuego EliminarJugadorDelJuego(string idJugador);
}


public sealed class ResultadoAccionJuego
{
    public bool FueExitosa { get; }
    public string Mensaje { get; }
    public string Datos { get; }

// Crea el objeto.
    public ResultadoAccionJuego(bool fueExitosa, string mensaje, string datos = "")
    {
        FueExitosa = fueExitosa;
        Mensaje = mensaje;
        Datos = datos;
    }
}
