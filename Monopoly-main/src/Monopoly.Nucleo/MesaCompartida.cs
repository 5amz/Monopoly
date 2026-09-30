using System;
using System.Net;
using System.Net.Sockets;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;

/// <summary>
/// Funde varios flujos IVistaJuego (uno por identidad TCP real conectada desde esta
/// pantalla) en una sola vista física. El servidor sigue exigiendo turno e identidad
/// ligada a cada socket; esta clase solo decide, del lado del cliente, cuál de las
/// conexiones internas de ESTA pantalla corresponde al jugador que tiene el turno,
/// cuando le toca a alguno de ellos.
/// </summary>
public sealed class VistaMesaCompartida
{
    private readonly IVistaJuego destino;
    private readonly EstadoBotones[] ultimos;
    private bool finMostrado;

    public int ActivoActual { get; private set; }

    public VistaMesaCompartida(IVistaJuego destino, int cantidadJugadores)
    {
        if (cantidadJugadores < 1) throw new ArgumentOutOfRangeException(nameof(cantidadJugadores));
        this.destino = destino;
        ultimos = new EstadoBotones[cantidadJugadores];
        for (int i = 0; i < cantidadJugadores; i++) ultimos[i] = new EstadoBotones();
    }

    // Un solo flujo de escenas impide que una identidad atrasada reponga la carta anterior.
    public void MostrarEscena(int indice, EscenaTablero escena)
    {
        if (indice == 0) destino.MostrarEscena(escena);
    }

    public void MostrarEfecto(int indice, EfectoMultimedia efecto)
    {
        if (indice == 0) destino.MostrarEfecto(efecto);
    }

    public void MostrarJugadores(int indice, ListaSimple<JugadorVista> jugadores, string idEnTurno) =>
        destino.MostrarJugadores(jugadores, idEnTurno);

    public void MostrarDados(int indice, int dado1, int dado2, int total, bool esHardware) =>
        destino.MostrarDados(dado1, dado2, total, esHardware);

    public void MostrarEstadoBotones(int indice, EstadoBotones estado)
    {
        ultimos[indice] = estado;
        if (estado.PuedeTirarDados || estado.PuedeComprar || estado.PuedeNoComprar || estado.PuedeTerminarTurno)
        {
            ActivoActual = indice;
        }

        destino.MostrarEstadoBotones(ultimos[ActivoActual]);
    }

    public void AgregarLineaLog(int indice, string linea) => destino.AgregarLineaLog(linea);

    public void MostrarCarta(int indice, string cancion, string texto, string efecto, int valor) =>
        destino.MostrarCarta(cancion, texto, efecto, valor);

    public void MostrarError(int indice, string codigo, string mensaje) => destino.MostrarError(codigo, mensaje);

    public void MostrarFinPartida(int indice, ResultadoPartida resultado)
    {
        // Todas las conexiones de esta pantalla reciben el mismo EVT_FIN_PARTIDA; mostrarlo una sola vez.
        if (finMostrado) return;
        finMostrado = true;
        destino.MostrarFinPartida(resultado);
    }

    public void MostrarEstadoConexion(int indice, EstadoConexion estado) => destino.MostrarEstadoConexion(estado);

    public void MostrarPagoPendiente(int indice, string idJugador, string descripcion, bool tarjetaRechazada) =>
        destino.MostrarPagoPendiente(idJugador, descripcion, tarjetaRechazada);

    public void OcultarPagoPendiente(int indice) => destino.OcultarPagoPendiente();
}

/// <summary>Adaptador liviano por identidad: reenvía cada llamada de IVistaJuego a la vista compartida con su índice.</summary>
public sealed class CanalVistaMesa : IVistaJuego
{
    private readonly int indice;
    private readonly VistaMesaCompartida compartida;

    public CanalVistaMesa(int indice, VistaMesaCompartida compartida)
    {
        this.indice = indice;
        this.compartida = compartida;
    }

    public void MostrarEfecto(EfectoMultimedia efecto) => compartida.MostrarEfecto(indice, efecto);
    public void MostrarEscena(EscenaTablero escena) => compartida.MostrarEscena(indice, escena);
    public void MostrarJugadores(ListaSimple<JugadorVista> jugadores, string idEnTurno) => compartida.MostrarJugadores(indice, jugadores, idEnTurno);
    public void MostrarDados(int dado1, int dado2, int total, bool esHardware) => compartida.MostrarDados(indice, dado1, dado2, total, esHardware);
    public void MostrarEstadoBotones(EstadoBotones estado) => compartida.MostrarEstadoBotones(indice, estado);
    public void AgregarLineaLog(string linea) => compartida.AgregarLineaLog(indice, linea);
    public void MostrarCarta(string cancion, string texto, string efecto, int valor) => compartida.MostrarCarta(indice, cancion, texto, efecto, valor);
    public void MostrarError(string codigo, string mensaje) => compartida.MostrarError(indice, codigo, mensaje);
    public void MostrarFinPartida(ResultadoPartida resultado) => compartida.MostrarFinPartida(indice, resultado);
    public void MostrarEstadoConexion(EstadoConexion estado) => compartida.MostrarEstadoConexion(indice, estado);
    public void MostrarPagoPendiente(string idJugador, string descripcion, bool tarjetaRechazada) =>
        compartida.MostrarPagoPendiente(indice, idJugador, descripcion, tarjetaRechazada);
    public void OcultarPagoPendiente() => compartida.OcultarPagoPendiente(indice);
}

/// <summary>
/// Enruta cada acción de esta pantalla hacia la conexión real (de esta misma pantalla)
/// del jugador en turno. El servidor no sabe que existe: sigue viendo sesiones TCP
/// normales con su propia identidad, validadas exactamente igual que en el modo de una
/// ventana por jugador. Cuando el turno le toca a un jugador de OTRA pantalla (otra
/// computadora), ningún botón de esta se habilita: solo actúa quien está frente a esa
/// pantalla, igual que en la mesa física.
/// </summary>
public sealed class ControladorMesa : IControladorJuego
{
    private readonly ControladorJuego[] controladores;
    private readonly VistaMesaCompartida vista;
    private readonly MotorAnimacion motor;

    public ControladorMesa(ControladorJuego[] controladores, VistaMesaCompartida vista, MotorAnimacion motor)
    {
        if (controladores.Length < 1) throw new ArgumentException("La pantalla necesita al menos una identidad.", nameof(controladores));
        this.controladores = controladores;
        this.vista = vista;
        this.motor = motor;
        // El servidor difunde cada recorrido a todas las identidades. Como esta mesa
        // comparte un solo motor, solo una de ellas debe encolarlo, sea de quien sea
        // el turno. Cada pantalla tiene su propio receptor y su propia animación.
        for (int i = 0; i < controladores.Length; i++)
            controladores[i].EncolaMovimientos = i == 0;
    }

    private ControladorJuego Activo => controladores[vista.ActivoActual];
    private ControladorJuego Lectura => controladores[0];

    public MotorAnimacion Motor => motor;
    public EstadoLocal Estado => Lectura.Estado;
    public void VincularHistorial(IVistaTransacciones historial) => Lectura.VincularHistorial(historial);
    public void CambiarTamano(double anchoDisponible, double altoDisponible)
    {
        foreach (var c in controladores) c.CambiarTamano(anchoDisponible, altoDisponible);
    }

    public void SolicitarTirarDados() => Activo.SolicitarTirarDados();
    public void SolicitarComprar() => Activo.SolicitarComprar();
    public void RechazarCompra() => Activo.RechazarCompra();
    public void TerminarTurno() => Activo.TerminarTurno();
    public void SimularTarjetaRemota(string uid) => Lectura.SimularTarjetaRemota(uid);
    public void AbrirHistorial() => Lectura.AbrirHistorial();
    public void FiltrarHistorial(string jugador, string tipo) => Lectura.FiltrarHistorial(jugador, tipo);
    public void HistorialAnterior() => Lectura.HistorialAnterior();
    public void HistorialSiguiente() => Lectura.HistorialSiguiente();
    public void HistorialPrimero() => Lectura.HistorialPrimero();
    public void HistorialUltimo() => Lectura.HistorialUltimo();
}

/// <summary>
/// Orquesta el registro de las identidades que le tocan a ESTA pantalla: puede alojar el
/// servidor local (una pantalla con las cuatro identidades, o la primera de un montaje en
/// dos computadoras) o unirse a un servidor ya alojado por otra pantalla (el resto de las
/// identidades, típicamente desde otra computadora en la misma red). Los ControladorJuego
/// resultantes se presentan en una sola vista (VistaMesaCompartida) sobre la misma FormJuego.
/// </summary>
public sealed class ControladorMesaConexion
{
    private readonly AnfitrionServidor anfitrion;
    private readonly IVistaConexion vistaConexion;
    private readonly IVistaJuego vistaJuego;
    private readonly ISincronizadorUI ui;
    private readonly MotorAnimacion motor;
    private ConexionServidor[]? conexiones;
    private ControladorJuego[]? controladores;
    private int puertoActual;
    private bool iniciando;
    private bool cerrando;
    private string[] nombres = Array.Empty<string>();
    private string ipActual = "";
    private bool pantallaAnfitriona;
    private int siguienteRegistro;
    private bool esperandoCorreccion;
    private bool partidaDisponible;
    private bool juegoAbierto;

    public Action<bool, string>? PrepararServidor { get; set; }
    public ControladorMesa? ControladorJuego { get; private set; }

    public event Action? PartidaDisponible;

    public ControladorMesaConexion(AnfitrionServidor anfitrion, IVistaConexion vistaConexion, IVistaJuego vistaJuego, ISincronizadorUI ui, MotorAnimacion motor)
    {
        this.anfitrion = anfitrion;
        this.vistaConexion = vistaConexion;
        this.vistaJuego = vistaJuego;
        this.ui = ui;
        this.motor = motor;
        anfitrion.ServidorListo += () => RegistrarIdentidades("127.0.0.1", esAnfitrionPantalla: true);
        anfitrion.ServidorFallo += texto =>
        {
            iniciando = false;
            ui.Ejecutar(() => vistaConexion.MostrarConexion(texto, true));
        };
    }

    // Comprueba el puerto y guarda los nombres de esta pantalla; comparte validación entre alojar y unirse.
    private bool PrepararNombresYPuerto(string[] nombresPantalla, string puerto, out int numero)
    {
        numero = 0;
        if (nombresPantalla.Length is < 1 or > 4)
            throw new ArgumentException("Esta pantalla debe representar entre 1 y 4 jugadores.", nameof(nombresPantalla));
        if (!int.TryParse(puerto, out numero) || numero < 1 || numero > 65535)
        {
            ui.Ejecutar(() => vistaConexion.MostrarConexion("El puerto debe estar entre 1 y 65535.", true));
            return false;
        }

        var normalizados = new string[nombresPantalla.Length];
        for (int i = 0; i < normalizados.Length; i++)
        {
            normalizados[i] = nombresPantalla[i].Trim();
            if (!AnalizadorMensajes.NombreValido(normalizados[i]))
            {
                ui.Ejecutar(() => vistaConexion.MostrarConexion("Cada nombre debe tener de 1 a 16 letras, números, espacios o guiones.", true));
                return false;
            }
            for (int j = 0; j < i; j++)
                if (normalizados[i].Equals(normalizados[j], StringComparison.OrdinalIgnoreCase))
                {
                    ui.Ejecutar(() => vistaConexion.MostrarConexion("Los nombres deben ser diferentes en toda la partida, sin importar la computadora ni las mayúsculas.", true));
                    return false;
                }
        }
        if (conexiones is not null)
        {
            if (normalizados.Length != nombres.Length)
            {
                ui.Ejecutar(() => vistaConexion.MostrarConexion("Conserve la cantidad de jugadores de esta pantalla y corrija el nombre rechazado.", true));
                return false;
            }
            for (int i = 0; i < siguienteRegistro; i++)
                if (normalizados[i] != nombres[i])
                {
                    string registrado = nombres[i];
                    ui.Ejecutar(() => vistaConexion.MostrarConexion("Conserve el nombre ya registrado: " + registrado + ". Cambie solo los jugadores pendientes.", true));
                    return false;
                }
        }
        nombres = normalizados;
        return true;
    }

    // Aloja el servidor local y registra las identidades de esta pantalla (el resto se une por red).
    public void AlojarMesa(string[] nombresPantalla, string puerto, bool usarPico, string puertoSerial)
    {
        if (cerrando || iniciando) return;
        if (conexiones is not null)
        {
            ReintentarRegistro(nombresPantalla, "127.0.0.1", puerto, true);
            return;
        }
        if (anfitrion.Activo) return;
        if (!PrepararNombresYPuerto(nombresPantalla, puerto, out int numero)) return;
        iniciando = true;
        puertoActual = numero;
        PrepararServidor?.Invoke(usarPico, puertoSerial);
        ui.Ejecutar(() => vistaConexion.MostrarConexion("Iniciando servidor de la mesa compartida…", false));
        anfitrion.Iniciar(numero);
    }

    // Se une a un servidor ya alojado por otra pantalla (otra computadora) y registra las identidades propias.
    public void UnirseMesa(string[] nombresPantalla, string ip, string puerto)
    {
        if (cerrando || iniciando) return;
        if (conexiones is not null)
        {
            ReintentarRegistro(nombresPantalla, ip.Trim(), puerto, false);
            return;
        }
        if (anfitrion.Activo) return;
        if (!PrepararNombresYPuerto(nombresPantalla, puerto, out int numero)) return;
        if (!IPAddress.TryParse(ip.Trim(), out var direccion) || direccion.AddressFamily != AddressFamily.InterNetwork)
        {
            ui.Ejecutar(() => vistaConexion.MostrarConexion("Ingrese una IPv4 válida como 192.168.0.12.", true));
            return;
        }

        iniciando = true;
        puertoActual = numero;
        ui.Ejecutar(() => vistaConexion.MostrarConexion("Conectando a " + direccion + ":" + numero + "…", false));
        RegistrarIdentidades(direccion.ToString(), esAnfitrionPantalla: false);
    }

    private void RegistrarIdentidades(string ip, bool esAnfitrionPantalla)
    {
        if (cerrando)
        {
            anfitrion.Detener();
            return;
        }

        ipActual = ip;
        pantallaAnfitriona = esAnfitrionPantalla;
        siguienteRegistro = 0;
        int cantidad = nombres.Length;
        var compartida = new VistaMesaCompartida(vistaJuego, cantidad);
        var nuevasConexiones = new ConexionServidor[cantidad];
        var nuevosControladores = new ControladorJuego[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            var red = new ConexionServidor();
            var canal = new CanalVistaMesa(i, compartida);
            nuevasConexiones[i] = red;
            nuevosControladores[i] = new ControladorJuego(red, canal, ui, motor);
            int indice = i;
            nuevosControladores[i].RegistroAceptado += () => ui.Ejecutar(() => ContinuarRegistro(indice));
            nuevosControladores[i].RegistroRechazado += mensaje => ui.Ejecutar(() =>
            {
                if (cerrando || indice != siguienteRegistro) return;
                iniciando = false;
                esperandoCorreccion = true;
                vistaConexion.MostrarConexion("Jugador " + (indice + 1) + " (" + nombres[indice] + "): " + mensaje + " Corrija el nombre y pulse Conectar esta pantalla.", true);
            });
        }

        conexiones = nuevasConexiones;
        controladores = nuevosControladores;
        ControladorJuego = new ControladorMesa(nuevosControladores, compartida, motor);

        // La primera identidad de esta pantalla recibe la misma difusión oficial que las
        // demás; basta con escucharla para saber cuándo la sala y la partida están listas.
        nuevosControladores[0].SalaActualizada += texto => ui.Ejecutar(() => vistaConexion.MostrarSala(texto));
        nuevosControladores[0].PartidaDisponible += () =>
        {
            // Vincular FormJuego inicia su Timer de Windows Forms: debe ocurrir en el
            // hilo con el bucle de mensajes, nunca en el lector TCP. Sin ese reloj las
            // animaciones quedan pendientes y bloquean los botones hasta un resync.
            // La vinculación y la apertura viajan juntas y en este orden.
            ui.Ejecutar(() =>
            {
                if (cerrando) return;
                partidaDisponible = true;
                AbrirSiLista();
            });
        };

        // Esperar cada OK evita registros parciales desordenados y permite corregir
        // un rechazo sin volver a conectar las identidades ya aceptadas.
        nuevosControladores[0].Preparar(nombres[0], esAnfitrionPantalla);
        nuevasConexiones[0].Conectar(ip, puertoActual);
    }

    private void ContinuarRegistro(int indice)
    {
        if (cerrando || indice != siguienteRegistro || conexiones is null) return;
        siguienteRegistro++;
        esperandoCorreccion = false;
        if (siguienteRegistro < conexiones.Length)
        {
            controladores![siguienteRegistro].Preparar(nombres[siguienteRegistro], false);
            conexiones[siguienteRegistro].Conectar(ipActual, puertoActual);
            return;
        }
        iniciando = false;
        vistaConexion.MostrarConexion("Jugadores registrados. Esperando que se complete la partida…", false);
        AbrirSiLista();
    }

    private void AbrirSiLista()
    {
        if (cerrando || juegoAbierto || !partidaDisponible || siguienteRegistro != nombres.Length) return;
        juegoAbierto = true;
        PartidaDisponible?.Invoke();
        vistaConexion.AbrirJuego();
    }

    private void ReintentarRegistro(string[] nuevosNombres, string ip, string puerto, bool alojar)
    {
        if (!esperandoCorreccion || controladores is null) return;
        if (alojar != pantallaAnfitriona || ip != ipActual || !int.TryParse(puerto, out int numero) || numero != puertoActual)
        {
            ui.Ejecutar(() => vistaConexion.MostrarConexion("Conserve el servidor y el puerto actuales; corrija solo los nombres pendientes.", true));
            return;
        }
        if (!PrepararNombresYPuerto(nuevosNombres, puerto, out _)) return;
        iniciando = true;
        esperandoCorreccion = false;
        vistaConexion.MostrarConexion("Registrando el nombre corregido…", false);
        controladores[siguienteRegistro].ReintentarRegistro(nombres[siguienteRegistro]);
    }

    // Pide confirmación (si aplica) y cierra las conexiones de esta pantalla y, si corresponde, el servidor local.
    public void SolicitarCierre()
    {
        if (cerrando) return;
        if (anfitrion.Activo || iniciando)
        {
            ui.Ejecutar(() => vistaConexion.ConfirmarCierre(
                "Cerrar esta pantalla afecta la partida de todos los jugadores. ¿Desea continuar?", ConfirmarCierre));
        }
        else
        {
            ConfirmarCierre(true);
        }
    }

    private void ConfirmarCierre(bool confirmar)
    {
        if (!confirmar || cerrando) return;
        cerrando = true;
        System.Threading.Tasks.Task.Run(() =>
        {
            if (controladores != null)
            {
                foreach (var c in controladores) c.Desconectar();
            }

            anfitrion.Detener();
            if (conexiones != null)
            {
                foreach (var red in conexiones) red.Dispose();
            }

            ui.Ejecutar(vistaConexion.CerrarAplicacion);
        });
    }
}
