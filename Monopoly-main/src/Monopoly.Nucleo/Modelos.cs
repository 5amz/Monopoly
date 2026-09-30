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

    // Guarda los datos del jugador y deja sus propiedades solo para lectura.
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

    // Guarda los datos de la casilla recibidos del servidor.
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

    // Guarda los datos recibidos de una transacción.
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

    // Guarda cuáles acciones permite el controlador.
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

    // Guarda el resultado que anunció el servidor.
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

    // Guarda el estado, el texto y el modo (HARDWARE/SIMULADOR) oficiales de la conexión.
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

    // Guarda la posición y el tamaño de un elemento.
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

    // Guarda el tipo, el lugar y el contenido que se va a dibujar.
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

    // Guarda los elementos que debe mostrar el tablero.
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

    // Guarda la transacción y las opciones que mostrará el historial.
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
    // Solo efectos de presentación; nunca ejecuta operaciones del banco.
    void MostrarEfecto(EfectoMultimedia efecto) { }

    // Muestra los elementos que preparó el núcleo.
    void MostrarEscena(EscenaTablero escena);
    // Muestra los jugadores y el id de quien tiene el turno.
    void MostrarJugadores(ListaSimple<JugadorVista> jugadores, string idEnTurno);
    // Muestra los dados y su origen recibidos del servidor.
    void MostrarDados(int dado1, int dado2, int total, bool esHardware);
    // Asigna a los botones las opciones que permite el controlador.
    void MostrarEstadoBotones(EstadoBotones estado);
    // Muestra el registro que preparó el núcleo.
    void AgregarLineaLog(string linea);
    // Muestra la carta recibida y su efecto oficial.
    void MostrarCarta(string cancion, string texto, string efecto, int valor);
    // Muestra el mensaje de error.
    void MostrarError(string codigo, string mensaje);
    // Muestra el ganador que anunció el servidor.
    void MostrarFinPartida(ResultadoPartida resultado);
    // Muestra el estado de la conexión y del turno.
    void MostrarEstadoConexion(EstadoConexion estado);
    // Pide de forma prominente la tarjeta RFID del jugador que debe pagar (compra, alquiler, impuesto o carta);
    // es la única confirmación de la acción, sin diálogo intermedio. tarjetaRechazada indica un UID incorrecto.
    void MostrarPagoPendiente(string idJugador, string descripcion, bool tarjetaRechazada);
    // Oculta el aviso de tarjeta pendiente una vez resuelto el pago.
    void OcultarPagoPendiente();
}

/// <summary>Superficie que usan las vistas de WinForms; permite sustituir un ControladorJuego real por un enrutador de mesa compartida.</summary>
public interface IControladorJuego
{
    MotorAnimacion Motor { get; }
    EstadoLocal? Estado => null;
    void VincularHistorial(IVistaTransacciones historial);
    void CambiarTamano(double anchoDisponible, double altoDisponible);
    void SolicitarTirarDados();
    // Pide la compra directamente: la tarjeta RFID es la única confirmación, sin diálogo intermedio.
    void SolicitarComprar();
    void RechazarCompra();
    void TerminarTurno();
    // Solo modo simulador: pide al servidor que procese esta tarjeta como si llegara por su propio puerto serial.
    void SimularTarjetaRemota(string uid);
    void AbrirHistorial();
    void FiltrarHistorial(string jugador, string tipo);
    void HistorialAnterior();
    void HistorialSiguiente();
    void HistorialPrimero();
    void HistorialUltimo();
}

public interface IVistaConexion
{
    // Muestra el aviso y activa o desactiva los botones de conexión.
    void MostrarConexion(string texto, bool puedeConectar);
    // Muestra los nombres de los jugadores conectados.
    void MostrarSala(string jugadores);
    // Abre la ventana del juego.
    void AbrirJuego();
    // Recibe o pide la confirmación antes de cerrar la partida.
    void ConfirmarCierre(string texto, Action<bool> respuesta);
    // Cierra las ventanas de la aplicación.
    void CerrarAplicacion();
}

public interface IVistaTransacciones
{
    // Muestra la transacción seleccionada y los botones de navegación.
    void MostrarHistorial(PaginaHistorial pagina);
    // Muestra las opciones para filtrar el historial.
    void MostrarFiltros(ListaSimple<string> jugadores, ListaSimple<string> tipos);
    // Abre la vista del historial.
    void Abrir();
}

public interface ISincronizadorUI
{
    // Ejecuta la acción en el hilo que usa la vista.
    void Ejecutar(Action accion);
}
