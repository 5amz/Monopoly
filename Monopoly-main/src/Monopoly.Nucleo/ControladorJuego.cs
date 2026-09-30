using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;
public sealed class ControladorJuego : IControladorJuego
{
    private readonly object bloqueo = new();
    private readonly ConexionServidor red;
    private readonly IVistaJuego vista;
    private readonly ISincronizadorUI ui;
    private readonly CalculadorEscena calculador = new();
    private IVistaTransacciones? vistaHistorial;
    private EstadoLocal estado = new();
    private CartaVisible? cartaVisible { get => estado.Carta; set => estado.Carta = value; }
    private int turnoVisual;
    private int turnoCarta;
    private string jugadorCarta = "";
    private string nombre = "";
    private string id = "";
    private string tokenReconexion = "";
    private bool pagoPendiente;
    private string idPagoPendiente = "";
    private string descripcionPago = "";
    private bool tarjetaRechazada;
    private bool identidadConfirmadaRFID;
    private string modo = "";
    private string filtroJugador = "TODOS";
    private string filtroTipo = "TODOS";
    private string detalleConexion = "Sin conexión";
    private bool anfitrion;
    private bool conectado;
    private bool iniciada;
    private bool finalizada;
    private bool recuperando = true;
    private bool dados;
    private bool oferta;
    private bool esperando;
    private bool actualizando;
    private int idOferta;
    private int cursorHistorial;
    private double ancho = 780;
    private double alto = 780;
    private EstadoBotones botones = new();
    private ListaSimple<TransaccionVista> filtradas = new();
    private bool ultimaCola;
    private int ultimoD1 = -1;
    private int ultimoD2 = -1;
    private int ultimoTotal = -1;
    private bool ultimoHardware;
    public MotorAnimacion Motor { get; }
    public EstadoLocal Estado => estado;
    // La mesa elige un único receptor de recorridos antes de conectar sus identidades.
    // Los otros controladores consultan el mismo motor para habilitar sus botones.
    internal bool EncolaMovimientos { get; set; } = true;

    public event Action<string>? SalaActualizada;
    public event Action? PartidaDisponible;
    public event Action? RegistroAceptado;
    public event Action<string>? RegistroRechazado;
    public bool Finalizada
    {
        get
        {
            lock (bloqueo)
            {
                return finalizada;
            }
        }
    }

    // Conecta la red, las animaciones y la vista con el controlador.
    public ControladorJuego(ConexionServidor red, IVistaJuego vista, ISincronizadorUI ui, MotorAnimacion motor)
    {
        this.red = red;
        this.vista = vista;
        this.ui = ui;
        Motor = motor;
        red.MensajeRecibido += Recibir;
        red.ConexionEstablecida += Establecida;
        red.ConexionPerdida += Perdida;
        motor.Actualizado += ActualizarAnimacion;
    }

    // Limpia los datos para entrar a una partida.
    public void Preparar(string nombreJugador, bool esAnfitrion)
    {
        lock (bloqueo)
        {
            if (nombre != nombreJugador) tokenReconexion = "";
            nombre = nombreJugador;
            anfitrion = esAnfitrion;
            id = "";
            estado = new EstadoLocal();
            cartaVisible = null;
            conectado = iniciada = finalizada = dados = oferta = esperando = false;
            identidadConfirmadaRFID = false;
            recuperando = true;
            ultimoD1 = ultimoD2 = -1;
            Motor.Reiniciar();
            Publicar();
        }
    }

    // Guarda la vista que mostrará las transacciones.
    public void VincularHistorial(IVistaTransacciones historial)
    {
        vistaHistorial = historial;
    }

    // Pide registrar al jugador cuando se abre la conexión.
    private void Establecida()
    {
        lock (bloqueo)
        {
            conectado = true;
            recuperando = true;
            iniciada = finalizada = dados = oferta = esperando = false;
            identidadConfirmadaRFID = false;
            Motor.Reiniciar();
            detalleConexion = "Conectado; recuperando estado";
            var solicitud = Mensaje.Crear("CONECTAR").Con("nombre", nombre);
            if (tokenReconexion.Length > 0) solicitud = solicitud.Con("token", tokenReconexion);
            red.Enviar(solicitud);
            Publicar();
        }
    }

    // Desactiva las acciones y muestra el problema de conexión.
    private void Perdida(string motivo)
    {
        lock (bloqueo)
        {
            conectado = false;
            recuperando = true;
            esperando = false;
            Motor.Reiniciar();
            detalleConexion = motivo;
            Log(motivo);
            Publicar();
        }
    }

    // Revisa el tipo de mensaje y actualiza lo que muestra el cliente.
    private void Recibir(Mensaje m)
    {
        lock (bloqueo)
        {
            try
            {
                if (m.Tipo is "EVT_PARTIDA_INICIADA" or "EVT_TURNO" or "EVT_DADOS" or "EVT_MOVIMIENTO" or "EVT_RECORRIDO" or "EVT_CASILLA" or "EVT_COMPRA" or "EVT_ALQUILER" or "EVT_ELIMINADO" or "EVT_FIN_PARTIDA")
                {
                    actualizando = true;
                }

                switch (m.Tipo)
                {
                    case "PONG":
                        return;
                    case "OK":
                        if (m.Obtener("accion") == "CONECTAR")
                        {
                            id = m.Obtener("idJugador");
                            tokenReconexion = m.Obtener("token");
                            RegistroAceptado?.Invoke();
                        }
                        else if (m.Obtener("accion") == "CONSULTAR_TRANSACCIONES")
                        {
                            estado.AplicarHistorial(m);
                            ActualizarHistorial();
                        }

                        break;
                    case "ERROR":
                        esperando = false;
                        if (m.Obtener("accion") == "CONECTAR" && id.Length == 0 && RegistroRechazado is not null)
                        {
                            // La mesa permite corregir el nombre en esta misma conexión.
                            // Una identidad rechazada no debe pintar su estado vacío sobre
                            // el tablero de los jugadores que sí están registrados.
                            recuperando = true;
                            detalleConexion = m.Obtener("mensaje");
                            RegistroRechazado.Invoke(detalleConexion);
                            return;
                        }
                        string errorVisible = PersonalizacionCiudad.TextoConCanciones(m.Obtener("mensaje"), estado.Casillas);
                        EjecutarVista(() => vista.MostrarError(m.Obtener("codigo"), errorVisible));
                        if (m.Obtener("accion")is "CONECTAR" or "DESCONECTAR")
                        {
                            conectado = false;
                            recuperando = true;
                            detalleConexion = m.Obtener("mensaje");
                            red.SuspenderReintentos();
                        }

                        break;
                    case "EVT_PARTIDA_INICIADA":
                        cartaVisible = null;
                        iniciada = true;
                        finalizada = false;
                        recuperando = true;
                        dados = oferta = false;
                        identidadConfirmadaRFID = false;
                        Motor.Reiniciar();
                        break;
                    case "EVT_TURNO":
                        turnoVisual = m.Entero("turno");
                        cartaVisible = null;
                        dados = oferta = false;
                        idOferta = 0;
                        identidadConfirmadaRFID = false;
                        // Limpia los dados en pantalla al pasar el turno; el próximo jugador
                        // todavía no tiró y no debe ver la tirada anterior. Hay que limpiar el
                        // motor también: si no, el próximo tick de animación redibujaría la
                        // tirada vieja porque Motor.TotalDados seguiría distinto de cero.
                        Motor.Dados(0, 0, false, 0);
                        ultimoD1 = ultimoD2 = ultimoTotal = 0;
                        ultimoHardware = false;
                        EjecutarVista(() => vista.MostrarDados(0, 0, 0, false));
                        break;
                    case "EVT_DADOS":
                        dados = true;
                        identidadConfirmadaRFID = false;
                        Motor.Dados(m.Entero("d1"), m.Entero("d2"), m.Obtener("origen") == "HARDWARE", m.Entero("total"));
                        break;
                    case "EVT_RECORRIDO":
                        if (EncolaMovimientos && !recuperando && estado.Casillas.Cantidad > 0)
                        {
                            Motor.Recorrido(m.Obtener("idJugador"), m.Entero("desde"), m.Entero("hasta"), m.Entero("pasos"), m.Booleano("salto"));
                        }

                        break;
                    case "EVT_MOVIMIENTO":
                        if (EncolaMovimientos && !recuperando && estado.Casillas.Cantidad > 0)
                        {
                            Motor.Mover(m.Obtener("idJugador"), m.Entero("desde"), m.Entero("hasta"), m.Booleano("pasoPorInicio"), estado.Casillas.Cantidad);
                        }

                        break;
                    case "EVT_CASILLA":
                        if (m.Obtener("idJugador") == id)
                        {
                            oferta = m.Obtener("accionDisponible") == "COMPRAR";
                            idOferta = m.Entero("idCasilla");
                            if (oferta && !recuperando)
                            {
                                Log("Oferta: " + m.Obtener("nombre") + " por ₡" + m.Dinero("precio").ToString("N0"));
                            }
                        }

                        break;
                    case "EVT_RFID":
                        // Un pago pendiente sin cambio de revision (el servidor no cobra ni
                        // reescribe el estado con un UID incorrecto): esta es la única señal
                        // de que la tarjeta acercada fue rechazada.
                        if (pagoPendiente)
                        {
                            tarjetaRechazada = m.Obtener("estado") == "INVALIDO";
                        }
                        else if (!dados)
                        {
                            // Fuera de un pago pendiente, la tarjeta correcta autoriza la
                            // siguiente tirada; el botón queda desactivado hasta entonces.
                            identidadConfirmadaRFID = m.Obtener("estado") == "VALIDO";
                        }

                        break;
                    case "EVT_CARTA":
                        string efecto = m.Obtener("efecto");
                        string cancion = PersonalizacionCiudad.CancionEvento(efecto);
                        turnoCarta = turnoVisual;
                        jugadorCarta = m.Obtener("idJugador");
                        cartaVisible = new CartaVisible(cancion, m.Obtener("texto"), efecto, m.Entero("valor"));
                        // El contexto recuperado restaura la carta, pero no repite su música.
                        string pistaCarta = PersonalizacionCiudad.ImagenEvento(efecto);
                        if (!recuperando && pistaCarta.Length > 0)
                            Efecto(new EfectoMultimedia(pistaCarta, jugadorCarta, Carta: cartaVisible));
                        EjecutarVista(() => vista.MostrarCarta(cancion, m.Obtener("texto"), efecto, m.Entero("valor")));
                        break;
                    case "EVT_COMPRA":
                        if (m.Obtener("idJugador") == id)
                        {
                            oferta = false;
                        }

                        break;
                    case "EVT_ALQUILER":
                        break;
                    case "EVT_TRANSACCION":
                        var transaccion = EstadoLocal.DesdeEvento(m);
                        estado.AgregarTransaccion(transaccion);
                        ActualizarHistorial();
                        break;
                    case "EVT_ELIMINADO":
                        if (!recuperando) Efecto(new EfectoMultimedia("eliminado", m.Obtener("idJugador")));
                        if (m.Obtener("idJugador") == id)
                        {
                            oferta = false;
                        }

                        break;
                    case "EVT_FIN_PARTIDA":
                        if (!recuperando && !finalizada) Efecto(new EfectoMultimedia("victoria", m.Obtener("idGanador")));
                        cartaVisible = null;
                        finalizada = true;
                        oferta = false;
                        var resultado = new ResultadoPartida(m.Obtener("idGanador"), m.Obtener("nombre"), m.Dinero("patrimonio"), m.Obtener("motivo"));
                        EjecutarVista(() => vista.MostrarFinPartida(resultado));
                        break;
                    case "EVT_ESTADO":
                        if (turnoCarta != m.Entero("turno") || jugadorCarta != m.Obtener("idJugadorActual"))
                            cartaVisible = null;
                        estado.AplicarSnapshot(m);
                        turnoVisual = estado.Turno;
                        iniciada = m.Obtener("estado") == "EN_CURSO";
                        finalizada = m.Obtener("estado") == "FINALIZADA";
                        dados = m.Booleano("dadosUsados");
                        oferta = m.Booleano("compraPendiente") && estado.IdJugadorActual == id;
                        pagoPendiente = m.Obtener("pagoPendiente") != "-";
                        idPagoPendiente = pagoPendiente ? m.Obtener("pagoPendiente") : "";
                        descripcionPago = pagoPendiente ? PersonalizacionCiudad.TextoConCanciones(m.Obtener("descripcionPago"), estado.Casillas) : "";
                        if (!pagoPendiente) tarjetaRechazada = false;
                        modo = m.Obtener("modo");
                        if (oferta && estado.Jugadores.Buscar(j => j.Id == id, out var propio))
                            idOferta = estado.Casillas.Obtener(propio.Posicion).Id;
                        recuperando = false;
                        esperando = false;
                        actualizando = false;
                        detalleConexion = pagoPendiente
                            ? "Acerque tarjeta: " + descripcionPago
                            : "Conectado";
                        var sala = new StringBuilder();
                        foreach (var j in estado.Jugadores)
                        {
                            sala.AppendLine(j.Id + " — " + j.Nombre);
                        }

                        string textoSala = sala.ToString();
                        SalaActualizada?.Invoke(textoSala);
                        if (iniciada || finalizada)
                        {
                            PartidaDisponible?.Invoke();
                        }

                        break;
                }

                if (m.Tipo != "EVT_ESTADO" && !(m.Tipo == "OK" && m.Obtener("accion") == "CONECTAR"))
                {
                    Log(m.ToString());
                }

                Publicar();
            }
            catch (Exception e)when (e is FormatException or InvalidOperationException or ArgumentOutOfRangeException)
            {
                recuperando = true;
                esperando = false;
                Motor.Reiniciar();
                Log("Mensaje rechazado: " + e.Message);
                EjecutarVista(() => vista.MostrarError("FORMATO_INVALIDO", e.Message));
                Publicar();
            }
        }
    }

    private void Efecto(EfectoMultimedia efecto) => EjecutarVista(() => vista.MostrarEfecto(efecto));

    // Agrega la hora y el mensaje al registro.
    private void Log(string linea)
    {
        linea = PersonalizacionCiudad.TextoConCanciones(linea, estado.Casillas);
        estado.RegistrarLog(DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " " + linea);
        string texto = estado.ObtenerLog();
        // Sin EjecutarVista a propósito: su manejo de fallos termina llamando aquí mismo, y
        // envolverlo hubiera arriesgado una recursión si el propio registro fallara.
        ui.Ejecutar(() => vista.AgregarLineaLog(texto));
    }

    // Ejecuta una actualización de la vista sin dejar que un fallo puntual (un caso real no
    // cubierto por esta vista, o una condición de carrera de la interfaz) se pierda en silencio
    // ni bloquee las demás actualizaciones. Antes, todas las llamadas de una misma publicación
    // viajaban juntas en un solo ui.Ejecutar: si una fallaba, ni esa ni las que la seguían en el
    // mismo bloque llegaban a pintarse, y con BeginInvoke sin EndInvoke la excepción desaparecía
    // sin ningún aviso — la pantalla podía quedar "congelada" para siempre sin ningún rastro.
    // Ahora cada campo se publica por separado: un fallo solo afecta a ese campo, se reporta en
    // el propio registro en pantalla, y la siguiente publicación (con cualquier mensaje nuevo del
    // servidor) lo vuelve a intentar de cero.
    private void EjecutarVista(Action accion)
    {
        ui.Ejecutar(() =>
        {
            try
            {
                accion();
            }
            catch (Exception ex)
            {
                Log("Fallo interno al actualizar la pantalla: " + ex.Message);
            }
        });
    }

    // Prepara los datos y botones que debe mostrar la vista.
    private void Publicar()
    {
        bool activo = estado.Jugadores.Buscar(j => j.Id == id && j.Activo, out _);
        bool habilitado = conectado && !actualizando && !recuperando && iniciada && !finalizada && !esperando && !pagoPendiente && !Motor.HayPendientes && activo && estado.IdJugadorActual == id;
        bool esperandoIdentidad = habilitado && !dados && !identidadConfirmadaRFID;
        // Tirar dados exige, en orden: tarjeta RFID del jugador en turno, luego el botón/pulsador.
        // Si no está esperando esa respuesta (identidad aún sin confirmar), el botón queda desactivado.
        var nuevos = new EstadoBotones(habilitado && !dados && identidadConfirmadaRFID, habilitado && oferta, habilitado && oferta, habilitado && dados && !oferta, conectado && !recuperando);
        botones = nuevos;
        var jugadores = estado.Jugadores;
        string turno = estado.IdJugadorActual;
        string barra = (esperandoIdentidad ? "Acerque su tarjeta para tirar los dados. " : "") + detalleConexion + " · " + modo + " · " + id + " · Turno " + estado.Turno + "/" + estado.MaxTurnos + " · " + turno + (anfitrion ? " · Anfitrión" : "");
        var conexion = new EstadoConexion(conectado, barra, modo, estado.Turno, estado.MaxTurnos);
        bool mostrarPago = pagoPendiente;
        string idPago = idPagoPendiente;
        string descripcion = descripcionPago;
        bool rechazada = tarjetaRechazada;
        // Cada campo por separado: si uno falla, los demás igual se actualizan.
        EjecutarVista(() => vista.MostrarJugadores(jugadores, turno));
        EjecutarVista(() => vista.MostrarEstadoBotones(nuevos));
        EjecutarVista(() => vista.MostrarEstadoConexion(conexion));
        if (mostrarPago) EjecutarVista(() => vista.MostrarPagoPendiente(idPago, descripcion, rechazada));
        else EjecutarVista(vista.OcultarPagoPendiente);
        Escena();
        DadosVisuales();
        ultimaCola = Motor.HayPendientes;
    }

    // Pide al calculador los elementos del tablero y los entrega a la vista.
    private void Escena()
    {
        var escena = calculador.Calcular(estado, Motor, ancho, alto, Motor.HayPendientes ? null : cartaVisible);
        EjecutarVista(() => vista.MostrarEscena(escena));
    }

    // Muestra los dados cuando cambia el resultado recibido.
    private void DadosVisuales()
    {
        var d = Motor.ObtenerDados();
        if (Motor.TotalDados == 0)
        {
            return;
        }

        if (d.Uno == ultimoD1 && d.Dos == ultimoD2 && d.Hardware == ultimoHardware && Motor.TotalDados == ultimoTotal)
        {
            return;
        }

        ultimoD1 = d.Uno;
        ultimoD2 = d.Dos;
        ultimoHardware = d.Hardware;
        int total = Motor.TotalDados;
        ultimoTotal = total;
        EjecutarVista(() => vista.MostrarDados(d.Uno, d.Dos, total, d.Hardware));
    }

    // Actualiza el tablero y los botones al avanzar una animación.
    private void ActualizarAnimacion()
    {
        lock (bloqueo)
        {
            if (ultimaCola != Motor.HayPendientes)
            {
                Publicar();
            }
            else
            {
                Escena();
                DadosVisuales();
            }
        }
    }

    // Guarda el tamaño disponible y vuelve a ubicar los elementos.
    public void CambiarTamano(double anchoDisponible, double altoDisponible)
    {
        lock (bloqueo)
        {
            ancho = Math.Max(100, anchoDisponible);
            alto = Math.Max(100, altoDisponible);
            Escena();
        }
    }

    // Envía una acción disponible y espera la respuesta del servidor.
    private void Solicitar(string tipo, bool permitido, int casilla = 0)
    {
        if (!permitido)
        {
            EjecutarVista(() => vista.MostrarError("ACCION_INVALIDA", "La acción no está disponible en este momento."));
            return;
        }

        var m = ConstructorMensajes.Accion(tipo, id);
        if (tipo == "COMPRAR_PROPIEDAD")
        {
            m = m.Con("idCasilla", casilla);
        }

        esperando = red.Enviar(m);
        if (!esperando)
        {
            EjecutarVista(() => vista.MostrarError("SIN_CONEXION", "No se pudo enviar la acción. Revise la conexión."));
        }

        Publicar();
    }

    // Pide al servidor que lance los dados.
    public void SolicitarTirarDados()
    {
        lock (bloqueo)
        {
            Solicitar("TIRAR_DADOS", botones.PuedeTirarDados);
        }
    }

    // Pide al servidor la compra; la tarjeta RFID es la única confirmación (no hay diálogo intermedio).
    public void SolicitarComprar()
    {
        lock (bloqueo)
        {
            Solicitar("COMPRAR_PROPIEDAD", botones.PuedeComprar, idOferta);
        }
    }

    // Avisa al servidor que el jugador no quiere comprar.
    public void RechazarCompra()
    {
        lock (bloqueo)
        {
            Solicitar("NO_COMPRAR", botones.PuedeNoComprar);
        }
    }

    // Pide al servidor que termine el turno.
    public void TerminarTurno()
    {
        lock (bloqueo)
        {
            Solicitar("TERMINAR_TURNO", botones.PuedeTerminarTurno);
        }
    }

    // Pide el estado completo para actualizar el cliente.
    public void ConsultarEstado()
    {
        lock (bloqueo)
        {
            if (!conectado || id.Length == 0)
            {
                return;
            }

            recuperando = true;
            dados = oferta = false;
            Motor.Reiniciar();
            red.Enviar(ConstructorMensajes.Accion("CONSULTAR_ESTADO", id));
            Publicar();
        }
    }

    // Corrige únicamente una identidad cuyo registro fue rechazado. Los jugadores
    // aceptados conservan su socket y su credencial; no se reinicia la mesa.
    internal void ReintentarRegistro(string nuevoNombre)
    {
        lock (bloqueo)
        {
            if (id.Length > 0) return;
            nombre = nuevoNombre;
            tokenReconexion = "";
            if (!red.Enviar(Mensaje.Crear("CONECTAR").Con("nombre", nombre)))
                RegistroRechazado?.Invoke("No hay conexión con el servidor. Espere la reconexión antes de intentar de nuevo.");
        }
    }

    // Pide al servidor que simule una tarjeta (solo modo simulador). Útil cuando esta pantalla
    // no aloja el servidor y por lo tanto no tiene un puerto serial propio que inyectar.
    public void SimularTarjetaRemota(string uid)
    {
        lock (bloqueo)
        {
            if (!conectado || id.Length == 0) return;
            red.Enviar(ConstructorMensajes.Accion("SIMULAR_RFID", id).Con("uid", uid));
        }
    }

    // Abre el historial y pide las transacciones.
    public void AbrirHistorial()
    {
        lock (bloqueo)
        {
            if (vistaHistorial is not null)
            {
                EjecutarVista(vistaHistorial.Abrir);
            }

            FiltrarHistorial("TODOS", "TODOS");
        }
    }

    // Pide las transacciones del jugador y del tipo elegidos.
    public void FiltrarHistorial(string jugador, string tipo)
    {
        lock (bloqueo)
        {
            filtroJugador = jugador.Length == 0 ? "TODOS" : jugador;
            filtroTipo = tipo.Length == 0 ? "TODOS" : tipo;
            cursorHistorial = 0;
            if (conectado && id.Length > 0)
            {
                red.Enviar(ConstructorMensajes.Accion("CONSULTAR_TRANSACCIONES", id).Con("tipo", filtroTipo).Con("jugador", filtroJugador));
            }

            ActualizarHistorial();
        }
    }

    // Recorre las transacciones y conserva las que cumplen los filtros.
    private void ActualizarHistorial()
    {
        filtradas = new ListaSimple<TransaccionVista>();
        var jugadores = new ListaSimple<string>();
        jugadores.Agregar("TODOS");
        foreach (var j in estado.Jugadores)
        {
            jugadores.Agregar(j.Id);
        }

        var tipos = new ListaSimple<string>();
        tipos.Agregar("TODOS");
        foreach (var t in estado.Transacciones)
        {
            if (!tipos.Buscar(x => x == t.Tipo, out _))
            {
                tipos.Agregar(t.Tipo);
            }

            if ((filtroJugador == "TODOS" || t.Origen == filtroJugador || t.Destino == filtroJugador) && (filtroTipo == "TODOS" || t.Tipo == filtroTipo))
            {
                filtradas.Agregar(t);
            }
        }

        if (!tipos.Buscar(x => x == filtroTipo, out _))
        {
            tipos.Agregar(filtroTipo);
        }

        jugadores.Congelar();
        tipos.Congelar();
        if (vistaHistorial is not null)
        {
            EjecutarVista(() => vistaHistorial.MostrarFiltros(jugadores, tipos));
        }

        cursorHistorial = Math.Clamp(cursorHistorial, 0, Math.Max(0, filtradas.Cantidad - 1));
        PublicarHistorial();
    }

    // Prepara el registro seleccionado y sus botones de navegación.
    private void PublicarHistorial()
    {
        string texto = "No hay transacciones con estos filtros.";
        if (filtradas.Cantidad > 0)
        {
            var t = filtradas.Obtener(cursorHistorial);
            texto = $"Turno {t.Turno} · Registro {t.Id}\n{DescribirTransaccion(t)}";
        }

        var pagina = new PaginaHistorial(texto, filtradas.Cantidad == 0 ? "0 / 0" : (cursorHistorial + 1) + " / " + filtradas.Cantidad, cursorHistorial > 0, cursorHistorial + 1 < filtradas.Cantidad);
        if (vistaHistorial is not null)
        {
            EjecutarVista(() => vistaHistorial.MostrarHistorial(pagina));
        }
    }

    // Redacta la transacción en una frase legible en vez de mostrar los códigos crudos.
    private string DescribirTransaccion(TransaccionVista t)
    {
        string monto = "₡" + t.Monto.ToString("N0");
        string origen = NombreDe(t.Origen);
        string destino = NombreDe(t.Destino);
        string descripcion = PersonalizacionCiudad.TextoConCanciones(t.Descripcion, estado.Casillas);
        return t.Tipo switch
        {
            "COMPRA_PROPIEDAD" => $"{origen} compró {descripcion} por {monto}.",
            "PAGO_ALQUILER" => $"{origen} pagó {monto} de alquiler a {destino} ({descripcion}).",
            "PASO_INICIO" => $"{origen} recibió {monto} por pasar por Inicio.",
            "CARTA_PREMIO" => $"{origen} ganó {monto}: {t.Descripcion}.",
            "CARTA_COBRO" => $"{origen} pagó {monto}: {t.Descripcion}.",
            "PagoAlBanco" => $"{origen} pagó {monto} al Banco: {t.Descripcion}.",
            "PagoEntreJugadores" => $"{origen} pagó {monto} a {destino}: {t.Descripcion}.",
            _ => $"{origen} → {destino} · {monto} ({t.Descripcion})"
        };
    }

    // Cambia un id crudo (Jx, BANCO, -) por el nombre del jugador cuando se conoce.
    private string NombreDe(string id) => id switch
    {
        "BANCO" => "el Banco",
        "-" => "nadie",
        _ => estado.Jugadores.Buscar(j => j.Id == id, out var jugador) ? jugador.Nombre : id
    };

    // Selecciona la transacción anterior.
    public void HistorialAnterior()
    {
        lock (bloqueo)
        {
            cursorHistorial = Math.Max(0, cursorHistorial - 1);
            PublicarHistorial();
        }
    }

    // Selecciona la siguiente transacción.
    public void HistorialSiguiente()
    {
        lock (bloqueo)
        {
            cursorHistorial = Math.Min(Math.Max(0, filtradas.Cantidad - 1), cursorHistorial + 1);
            PublicarHistorial();
        }
    }

    // Selecciona la transacción más antigua.
    public void HistorialPrimero()
    {
        lock (bloqueo)
        {
            cursorHistorial = 0;
            PublicarHistorial();
        }
    }

    // Selecciona la transacción más reciente.
    public void HistorialUltimo()
    {
        lock (bloqueo)
        {
            cursorHistorial = Math.Max(0, filtradas.Cantidad - 1);
            PublicarHistorial();
        }
    }

    // Avisa de la salida y cierra la conexión.
    public void Desconectar()
    {
        lock (bloqueo)
        {
            if (conectado && id.Length > 0)
            {
                red.EnviarCierre(ConstructorMensajes.Accion("DESCONECTAR", id));
            }
        }

        red.Desconectar();
    }
}
