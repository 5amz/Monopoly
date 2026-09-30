using System;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;
public sealed class JugadorVista
{
    public string Id { get; }
    public string Nombre { get; }
    public decimal Saldo { get; }
    public int Posicion { get; }
    public bool Activo { get; }
    public ListaSimple<int> Propiedades { get; }

    
// Crea el objeto.
    public JugadorVista(string id, string nombre, decimal saldo, int posicion, bool activo, ListaSimple<int> propiedades)
    {
        Id = id;
        Nombre = nombre;
        Saldo = saldo;
        Posicion = posicion;
        Activo = activo;
        Propiedades = propiedades.Congelar();
    }
}

public sealed class CasillaVista
{
    public int Id { get; }
    public string Tipo { get; }
    public string Nombre { get; }
    public decimal Precio { get; }
    public decimal Alquiler { get; }
    public string IdPropietario { get; }
    public string Cancion { get; }
    public string Artista { get; }
    public string Imagen { get; }
    public FragmentoMusical? Musica { get; internal set; }
    internal ConfiguracionMusica? ConfiguracionMusical { get; set; }

    
// Crea el objeto.
    public CasillaVista(int id, string tipo, string nombre, decimal precio, decimal alquiler, string propietario)
    {
        Id = id;
        Tipo = tipo;
        Nombre = nombre;
        Precio = precio;
        Alquiler = alquiler;
        IdPropietario = propietario;
        Cancion = PersonalizacionCiudad.Cancion(id);
        Artista = PersonalizacionCiudad.Artista(id);
        Imagen = "casilla_" + id.ToString("D2");
    }
}

public sealed class TransaccionVista
{
    public int Id { get; }
    public int Turno { get; }
    public string Tipo { get; }
    public string Origen { get; }
    public string Destino { get; }
    public decimal Monto { get; }
    public string Descripcion { get; }

    
// Crea el objeto.
    public TransaccionVista(int Id, int Turno, string Tipo, string Origen, string Destino, decimal Monto, string Descripcion)
    {
        this.Id = Id;
        this.Turno = Turno;
        this.Tipo = Tipo;
        this.Origen = Origen;
        this.Destino = Destino;
        this.Monto = Monto;
        this.Descripcion = Descripcion;
    }
}

public sealed class EstadoBotones
{
    public bool PuedeTirarDados { get; }
    public bool PuedeComprar { get; }
    public bool PuedeNoComprar { get; }
    public bool PuedeTerminarTurno { get; }
    public bool PuedeVerHistorial { get; }

    
// Crea el objeto.
    public EstadoBotones(bool PuedeTirarDados = false, bool PuedeComprar = false, bool PuedeNoComprar = false, bool PuedeTerminarTurno = false, bool PuedeVerHistorial = false)
    {
        this.PuedeTirarDados = PuedeTirarDados;
        this.PuedeComprar = PuedeComprar;
        this.PuedeNoComprar = PuedeNoComprar;
        this.PuedeTerminarTurno = PuedeTerminarTurno;
        this.PuedeVerHistorial = PuedeVerHistorial;
    }
}

public sealed class ResultadoPartida
{
    public string IdGanador { get; }
    public string Nombre { get; }
    public decimal Patrimonio { get; }
    public string Motivo { get; }

    
// Crea el objeto.
    public ResultadoPartida(string IdGanador, string Nombre, decimal Patrimonio, string Motivo)
    {
        this.IdGanador = IdGanador;
        this.Nombre = Nombre;
        this.Patrimonio = Patrimonio;
        this.Motivo = Motivo;
    }
}

public sealed class EstadoConexion
{
    public bool Conectado { get; }
    public string Texto { get; }
    public string Modo { get; }
    public int Turno { get; }
    public int MaxTurnos { get; }

    
// Crea el objeto.
    public EstadoConexion(bool Conectado, string Texto, string Modo = "", int Turno = 0, int MaxTurnos = 0)
    {
        this.Conectado = Conectado;
        this.Texto = Texto;
        this.Modo = Modo;
        this.Turno = Turno;
        this.MaxTurnos = MaxTurnos;
    }
}

public sealed class Rectangulo
{
    public double X { get; }
    public double Y { get; }
    public double Ancho { get; }
    public double Alto { get; }

    
// Crea el objeto.
    public Rectangulo(double X, double Y, double Ancho, double Alto)
    {
        this.X = X;
        this.Y = Y;
        this.Ancho = Ancho;
        this.Alto = Alto;
    }
}

public sealed class ElementoEscena
{
    public string Tipo { get; }
    public Rectangulo Area { get; }
    public string Contenido { get; }
    public string Destino { get; }

    
// Crea el objeto.
    public ElementoEscena(string Tipo, Rectangulo Area, string Contenido, string Destino = "")
    {
        this.Tipo = Tipo;
        this.Area = Area;
        this.Contenido = Contenido;
        this.Destino = Destino;
    }
}

public sealed class EscenaTablero
{
    public ListaSimple<ElementoEscena> Elementos { get; }

    
// Crea el objeto.
    public EscenaTablero(ListaSimple<ElementoEscena> Elementos)
    {
        this.Elementos = Elementos;
    }
}

public sealed class PaginaHistorial
{
    public string Texto { get; }
    public string Contador { get; }
    public bool PuedeAnterior { get; }
    public bool PuedeSiguiente { get; }

    
// Crea el objeto.
    public PaginaHistorial(string Texto, string Contador, bool PuedeAnterior, bool PuedeSiguiente)
    {
        this.Texto = Texto;
        this.Contador = Contador;
        this.PuedeAnterior = PuedeAnterior;
        this.PuedeSiguiente = PuedeSiguiente;
    }
}

public sealed record EfectoMultimedia(string Tipo, string Jugador = "", CartaVisible? Carta = null);

public interface IVistaJuego
{
    
// Ejecuta MostrarEfecto.
    void MostrarEfecto(EfectoMultimedia efecto) { }

    
// Ejecuta MostrarEscena.
    void MostrarEscena(EscenaTablero escena);
    
// Ejecuta MostrarJugadores.
    void MostrarJugadores(ListaSimple<JugadorVista> jugadores, string idEnTurno);
    
// Ejecuta MostrarDados.
    void MostrarDados(int dado1, int dado2, int total, bool esHardware);
    
// Ejecuta MostrarEstadoBotones.
    void MostrarEstadoBotones(EstadoBotones estado);
    
// Ejecuta AgregarLineaLog.
    void AgregarLineaLog(string linea);
    
// Ejecuta MostrarCarta.
    void MostrarCarta(string cancion, string texto, string efecto, int valor);
    
// Ejecuta MostrarError.
    void MostrarError(string codigo, string mensaje);
    
// Ejecuta MostrarFinPartida.
    void MostrarFinPartida(ResultadoPartida resultado);
    
// Ejecuta MostrarEstadoConexion.
    void MostrarEstadoConexion(EstadoConexion estado);
    
    
// Ejecuta MostrarPagoPendiente.
    void MostrarPagoPendiente(string idJugador, string descripcion, bool tarjetaRechazada);
    
// Ejecuta OcultarPagoPendiente.
    void OcultarPagoPendiente();
}


public interface IControladorJuego
{
    MotorAnimacion Motor { get; }
    EstadoLocal? Estado => null;
// Ejecuta VincularHistorial.
    void VincularHistorial(IVistaTransacciones historial);
// Ejecuta CambiarTamano.
    void CambiarTamano(double anchoDisponible, double altoDisponible);
// Ejecuta SolicitarTirarDados.
    void SolicitarTirarDados();
    
// Ejecuta SolicitarComprar.
    void SolicitarComprar();
// Ejecuta RechazarCompra.
    void RechazarCompra();
// Ejecuta TerminarTurno.
    void TerminarTurno();
    
// Ejecuta SimularTarjetaRemota.
    void SimularTarjetaRemota(string uid);
// Ejecuta AbrirHistorial.
    void AbrirHistorial();
// Ejecuta FiltrarHistorial.
    void FiltrarHistorial(string jugador, string tipo);
// Ejecuta HistorialAnterior.
    void HistorialAnterior();
// Ejecuta HistorialSiguiente.
    void HistorialSiguiente();
// Ejecuta HistorialPrimero.
    void HistorialPrimero();
// Ejecuta HistorialUltimo.
    void HistorialUltimo();
}

public interface IVistaConexion
{
    
// Ejecuta MostrarConexion.
    void MostrarConexion(string texto, bool puedeConectar);
    
// Ejecuta MostrarSala.
    void MostrarSala(string jugadores);
    
// Ejecuta AbrirJuego.
    void AbrirJuego();
    
// Ejecuta ConfirmarCierre.
    void ConfirmarCierre(string texto, Action<bool> respuesta);
    
// Ejecuta CerrarAplicacion.
    void CerrarAplicacion();
}

public interface IVistaTransacciones
{
    
// Ejecuta MostrarHistorial.
    void MostrarHistorial(PaginaHistorial pagina);
    
// Ejecuta MostrarFiltros.
    void MostrarFiltros(ListaSimple<string> jugadores, ListaSimple<string> tipos);
    
// Ejecuta Abrir.
    void Abrir();
}

public interface ISincronizadorUI
{
    
// Ejecuta Ejecutar.
    void Ejecutar(Action accion);
}
