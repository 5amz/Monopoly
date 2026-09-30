using Monopoly.Administracion;

namespace Monopoly;


public sealed class Juego
{
    public ServidorJuego EstadoOficial { get; }
    public CoordinadorPartidaTablero Coordinador { get; }

// Crea el objeto.
    public Juego(ServidorJuego estadoOficial, CoordinadorPartidaTablero coordinador)
    {
        EstadoOficial = estadoOficial ?? throw new ArgumentNullException(nameof(estadoOficial));
        Coordinador = coordinador ?? throw new ArgumentNullException(nameof(coordinador));
    }

// Ejecuta RegistrarJugador.
    public ResultadoOperacion RegistrarJugador(string id, string nombre) => EstadoOficial.RegistrarJugador(id, nombre);
// Ejecuta Iniciar.
    public void Iniciar() => EstadoOficial.MarcarPartidaIniciada();
// Ejecuta TirarDados.
    public ResultadoAccionJuego TirarDados(string id) => Coordinador.TirarDados(id);
// Ejecuta ComprarPropiedad.
    public ResultadoAccionJuego ComprarPropiedad(string id) => Coordinador.ComprarPropiedad(id);
// Ejecuta NoComprar.
    public ResultadoAccionJuego NoComprar(string id) => Coordinador.NoComprarPropiedad(id);
// Ejecuta TerminarTurno.
    public ResultadoAccionJuego TerminarTurno(string id) => Coordinador.TerminarTurno(id);
// Ejecuta ExportarHistorial.
    public void ExportarHistorial(string ruta) => EstadoOficial.ExportarTransacciones(ruta);
}
