using Monopoly.Administracion;

namespace Monopoly;

/// <summary>Fachada de la rúbrica; delega sin mantener otro estado de partida.</summary>
public sealed class Juego
{
    public ServidorJuego EstadoOficial { get; }
    public CoordinadorPartidaTablero Coordinador { get; }

    public Juego(ServidorJuego estadoOficial, CoordinadorPartidaTablero coordinador)
    {
        EstadoOficial = estadoOficial ?? throw new ArgumentNullException(nameof(estadoOficial));
        Coordinador = coordinador ?? throw new ArgumentNullException(nameof(coordinador));
    }

    public ResultadoOperacion RegistrarJugador(string id, string nombre) => EstadoOficial.RegistrarJugador(id, nombre);
    public void Iniciar() => EstadoOficial.MarcarPartidaIniciada();
    public ResultadoAccionJuego TirarDados(string id) => Coordinador.TirarDados(id);
    public ResultadoAccionJuego ComprarPropiedad(string id) => Coordinador.ComprarPropiedad(id);
    public ResultadoAccionJuego NoComprar(string id) => Coordinador.NoComprarPropiedad(id);
    public ResultadoAccionJuego TerminarTurno(string id) => Coordinador.TerminarTurno(id);
    public void ExportarHistorial(string ruta) => EstadoOficial.ExportarTransacciones(ruta);
}
