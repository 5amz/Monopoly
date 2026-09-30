using System;
using System.Net;
using System.Net.Sockets;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;








public sealed class VistaMesaCompartida
{
    private readonly IVistaJuego destino;
    private readonly EstadoBotones[] ultimos;
    private bool finMostrado;

    public int ActivoActual { get; private set; }

// Crea el objeto.
    public VistaMesaCompartida(IVistaJuego destino, int cantidadJugadores)
    {
        if (cantidadJugadores < 1) throw new ArgumentOutOfRangeException(nameof(cantidadJugadores));
        this.destino = destino;
        ultimos = new EstadoBotones[cantidadJugadores];
        for (int i = 0; i < cantidadJugadores; i++) ultimos[i] = new EstadoBotones();
    }

    
// Ejecuta MostrarEscena.
    public void MostrarEscena(int indice, EscenaTablero escena)
    {
        if (indice == 0) destino.MostrarEscena(escena);
    }

// Ejecuta MostrarEfecto.
    public void MostrarEfecto(int indice, EfectoMultimedia efecto)
    {
        if (indice == 0) destino.MostrarEfecto(efecto);
    }

// Ejecuta MostrarJugadores.
    public void MostrarJugadores(int indice, ListaSimple<JugadorVista> jugadores, string idEnTurno) =>
        destino.MostrarJugadores(jugadores, idEnTurno);

// Ejecuta MostrarDados.
    public void MostrarDados(int indice, int dado1, int dado2, int total, bool esHardware) =>
        destino.MostrarDados(dado1, dado2, total, esHardware);

// Ejecuta MostrarEstadoBotones.
    public void MostrarEstadoBotones(int indice, EstadoBotones estado)
    {
        ultimos[indice] = estado;
        if (estado.PuedeTirarDados || estado.PuedeComprar || estado.PuedeNoComprar || estado.PuedeTerminarTurno)
        {
            ActivoActual = indice;
        }

        destino.MostrarEstadoBotones(ultimos[ActivoActual]);
    }

// Ejecuta AgregarLineaLog.
    public void AgregarLineaLog(int indice, string linea) => destino.AgregarLineaLog(linea);

// Ejecuta MostrarCarta.
    public void MostrarCarta(int indice, string cancion, string texto, string efecto, int valor) =>
        destino.MostrarCarta(cancion, texto, efecto, valor);

// Ejecuta MostrarError.
    public void MostrarError(int indice, string codigo, string mensaje) => destino.MostrarError(codigo, mensaje);

// Ejecuta MostrarFinPartida.
    public void MostrarFinPartida(int indice, ResultadoPartida resultado)
    {
        
        if (finMostrado) return;
        finMostrado = true;
        destino.MostrarFinPartida(resultado);
    }

// Ejecuta MostrarEstadoConexion.
    public void MostrarEstadoConexion(int indice, EstadoConexion estado) => destino.MostrarEstadoConexion(estado);

// Ejecuta MostrarPagoPendiente.
    public void MostrarPagoPendiente(int indice, string idJugador, string descripcion, bool tarjetaRechazada) =>
        destino.MostrarPagoPendiente(idJugador, descripcion, tarjetaRechazada);

// Ejecuta OcultarPagoPendiente.
    public void OcultarPagoPendiente(int indice) => destino.OcultarPagoPendiente();
}


public sealed class CanalVistaMesa : IVistaJuego
{
    private readonly int indice;
    private readonly VistaMesaCompartida compartida;

// Crea el objeto.
    public CanalVistaMesa(int indice, VistaMesaCompartida compartida)
    {
        this.indice = indice;
        this.compartida = compartida;
    }

// Ejecuta MostrarEfecto.
    public void MostrarEfecto(EfectoMultimedia efecto) => compartida.MostrarEfecto(indice, efecto);
// Ejecuta MostrarEscena.
    public void MostrarEscena(EscenaTablero escena) => compartida.MostrarEscena(indice, escena);
// Ejecuta MostrarJugadores.
    public void MostrarJugadores(ListaSimple<JugadorVista> jugadores, string idEnTurno) => compartida.MostrarJugadores(indice, jugadores, idEnTurno);
// Ejecuta MostrarDados.
    public void MostrarDados(int dado1, int dado2, int total, bool esHardware) => compartida.MostrarDados(indice, dado1, dado2, total, esHardware);
// Ejecuta MostrarEstadoBotones.
    public void MostrarEstadoBotones(EstadoBotones estado) => compartida.MostrarEstadoBotones(indice, estado);
// Ejecuta AgregarLineaLog.
    public void AgregarLineaLog(string linea) => compartida.AgregarLineaLog(indice, linea);
// Ejecuta MostrarCarta.
    public void MostrarCarta(string cancion, string texto, string efecto, int valor) => compartida.MostrarCarta(indice, cancion, texto, efecto, valor);
// Ejecuta MostrarError.
    public void MostrarError(string codigo, string mensaje) => compartida.MostrarError(indice, codigo, mensaje);
// Ejecuta MostrarFinPartida.
    public void MostrarFinPartida(ResultadoPartida resultado) => compartida.MostrarFinPartida(indice, resultado);
// Ejecuta MostrarEstadoConexion.
    public void MostrarEstadoConexion(EstadoConexion estado) => compartida.MostrarEstadoConexion(indice, estado);
// Ejecuta MostrarPagoPendiente.
    public void MostrarPagoPendiente(string idJugador, string descripcion, bool tarjetaRechazada) =>
        compartida.MostrarPagoPendiente(indice, idJugador, descripcion, tarjetaRechazada);
// Ejecuta OcultarPagoPendiente.
    public void OcultarPagoPendiente() => compartida.OcultarPagoPendiente(indice);
}









public sealed class ControladorMesa : IControladorJuego
{
    private readonly ControladorJuego[] controladores;
    private readonly VistaMesaCompartida vista;
    private readonly MotorAnimacion motor;

// Crea el objeto.
    public ControladorMesa(ControladorJuego[] controladores, VistaMesaCompartida vista, MotorAnimacion motor)
    {
        if (controladores.Length < 1) throw new ArgumentException("La pantalla necesita al menos una identidad.", nameof(controladores));
        this.controladores = controladores;
        this.vista = vista;
        this.motor = motor;
        
        
        
        for (int i = 0; i < controladores.Length; i++)
            controladores[i].EncolaMovimientos = i == 0;
    }

    private ControladorJuego Activo => controladores[vista.ActivoActual];
    private ControladorJuego Lectura => controladores[0];

    public MotorAnimacion Motor => motor;
    public EstadoLocal Estado => Lectura.Estado;
// Ejecuta VincularHistorial.
    public void VincularHistorial(IVistaTransacciones historial) => Lectura.VincularHistorial(historial);
// Ejecuta CambiarTamano.
    public void CambiarTamano(double anchoDisponible, double altoDisponible)
    {
        foreach (var c in controladores) c.CambiarTamano(anchoDisponible, altoDisponible);
    }

// Ejecuta SolicitarTirarDados.
    public void SolicitarTirarDados() => Activo.SolicitarTirarDados();
// Ejecuta SolicitarComprar.
    public void SolicitarComprar() => Activo.SolicitarComprar();
// Ejecuta RechazarCompra.
    public void RechazarCompra() => Activo.RechazarCompra();
// Ejecuta TerminarTurno.
    public void TerminarTurno() => Activo.TerminarTurno();
// Ejecuta SimularTarjetaRemota.
    public void SimularTarjetaRemota(string uid) => Lectura.SimularTarjetaRemota(uid);
// Ejecuta AbrirHistorial.
    public void AbrirHistorial() => Lectura.AbrirHistorial();
// Ejecuta FiltrarHistorial.
    public void FiltrarHistorial(string jugador, string tipo) => Lectura.FiltrarHistorial(jugador, tipo);
// Ejecuta HistorialAnterior.
    public void HistorialAnterior() => Lectura.HistorialAnterior();
// Ejecuta HistorialSiguiente.
    public void HistorialSiguiente() => Lectura.HistorialSiguiente();
// Ejecuta HistorialPrimero.
    public void HistorialPrimero() => Lectura.HistorialPrimero();
// Ejecuta HistorialUltimo.
    public void HistorialUltimo() => Lectura.HistorialUltimo();
}








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

// Crea el objeto.
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

    
// Ejecuta PrepararNombresYPuerto.
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

    
// Ejecuta AlojarMesa.
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

    
// Ejecuta UnirseMesa.
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

// Ejecuta RegistrarIdentidades.
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

        
        
        nuevosControladores[0].SalaActualizada += texto => ui.Ejecutar(() => vistaConexion.MostrarSala(texto));
        nuevosControladores[0].PartidaDisponible += () =>
        {
            
            
            
            
            ui.Ejecutar(() =>
            {
                if (cerrando) return;
                partidaDisponible = true;
                AbrirSiLista();
            });
        };

        
        
        nuevosControladores[0].Preparar(nombres[0], esAnfitrionPantalla);
        nuevasConexiones[0].Conectar(ip, puertoActual);
    }

// Ejecuta ContinuarRegistro.
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

// Ejecuta AbrirSiLista.
    private void AbrirSiLista()
    {
        if (cerrando || juegoAbierto || !partidaDisponible || siguienteRegistro != nombres.Length) return;
        juegoAbierto = true;
        PartidaDisponible?.Invoke();
        vistaConexion.AbrirJuego();
    }

// Ejecuta ReintentarRegistro.
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

    
// Ejecuta SolicitarCierre.
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

// Ejecuta ConfirmarCierre.
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
