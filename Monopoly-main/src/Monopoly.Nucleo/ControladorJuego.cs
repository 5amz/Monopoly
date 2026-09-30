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

    
// Crea el objeto.
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

    
// Ejecuta Preparar.
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

    
// Ejecuta VincularHistorial.
    public void VincularHistorial(IVistaTransacciones historial)
    {
        vistaHistorial = historial;
    }

    
// Ejecuta Establecida.
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

    
// Ejecuta Perdida.
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

    
// Ejecuta Recibir.
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
                        
                        
                        
                        if (pagoPendiente)
                        {
                            tarjetaRechazada = m.Obtener("estado") == "INVALIDO";
                        }
                        else if (!dados)
                        {
                            
                            
                            identidadConfirmadaRFID = m.Obtener("estado") == "VALIDO";
                        }

                        break;
                    case "EVT_CARTA":
                        string efecto = m.Obtener("efecto");
                        string cancion = PersonalizacionCiudad.CancionEvento(efecto);
                        turnoCarta = turnoVisual;
                        jugadorCarta = m.Obtener("idJugador");
                        cartaVisible = new CartaVisible(cancion, m.Obtener("texto"), efecto, m.Entero("valor"));
                        
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

// Ejecuta Efecto.
    private void Efecto(EfectoMultimedia efecto) => EjecutarVista(() => vista.MostrarEfecto(efecto));

    
// Ejecuta Log.
    private void Log(string linea)
    {
        linea = PersonalizacionCiudad.TextoConCanciones(linea, estado.Casillas);
        estado.RegistrarLog(DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " " + linea);
        string texto = estado.ObtenerLog();
        
        
        ui.Ejecutar(() => vista.AgregarLineaLog(texto));
    }

    
    
    
    
    
    
    
    
    
// Ejecuta EjecutarVista.
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

    
// Ejecuta Publicar.
    private void Publicar()
    {
        bool activo = estado.Jugadores.Buscar(j => j.Id == id && j.Activo, out _);
        bool habilitado = conectado && !actualizando && !recuperando && iniciada && !finalizada && !esperando && !pagoPendiente && !Motor.HayPendientes && activo && estado.IdJugadorActual == id;
        bool esperandoIdentidad = habilitado && !dados && !identidadConfirmadaRFID;
        
        
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
        
        EjecutarVista(() => vista.MostrarJugadores(jugadores, turno));
        EjecutarVista(() => vista.MostrarEstadoBotones(nuevos));
        EjecutarVista(() => vista.MostrarEstadoConexion(conexion));
        if (mostrarPago) EjecutarVista(() => vista.MostrarPagoPendiente(idPago, descripcion, rechazada));
        else EjecutarVista(vista.OcultarPagoPendiente);
        Escena();
        DadosVisuales();
        ultimaCola = Motor.HayPendientes;
    }

    
// Ejecuta Escena.
    private void Escena()
    {
        var escena = calculador.Calcular(estado, Motor, ancho, alto, Motor.HayPendientes ? null : cartaVisible);
        EjecutarVista(() => vista.MostrarEscena(escena));
    }

    
// Ejecuta DadosVisuales.
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

    
// Ejecuta ActualizarAnimacion.
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

    
// Ejecuta CambiarTamano.
    public void CambiarTamano(double anchoDisponible, double altoDisponible)
    {
        lock (bloqueo)
        {
            ancho = Math.Max(100, anchoDisponible);
            alto = Math.Max(100, altoDisponible);
            Escena();
        }
    }

    
// Ejecuta Solicitar.
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

    
// Ejecuta SolicitarTirarDados.
    public void SolicitarTirarDados()
    {
        lock (bloqueo)
        {
            Solicitar("TIRAR_DADOS", botones.PuedeTirarDados);
        }
    }

    
// Ejecuta SolicitarComprar.
    public void SolicitarComprar()
    {
        lock (bloqueo)
        {
            Solicitar("COMPRAR_PROPIEDAD", botones.PuedeComprar, idOferta);
        }
    }

    
// Ejecuta RechazarCompra.
    public void RechazarCompra()
    {
        lock (bloqueo)
        {
            Solicitar("NO_COMPRAR", botones.PuedeNoComprar);
        }
    }

    
// Ejecuta TerminarTurno.
    public void TerminarTurno()
    {
        lock (bloqueo)
        {
            Solicitar("TERMINAR_TURNO", botones.PuedeTerminarTurno);
        }
    }

    
// Ejecuta ConsultarEstado.
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

    
    
// Ejecuta ReintentarRegistro.
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

    
    
// Ejecuta SimularTarjetaRemota.
    public void SimularTarjetaRemota(string uid)
    {
        lock (bloqueo)
        {
            if (!conectado || id.Length == 0) return;
            red.Enviar(ConstructorMensajes.Accion("SIMULAR_RFID", id).Con("uid", uid));
        }
    }

    
// Ejecuta AbrirHistorial.
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

    
// Ejecuta FiltrarHistorial.
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

    
// Ejecuta ActualizarHistorial.
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

    
// Ejecuta PublicarHistorial.
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

    
// Ejecuta DescribirTransaccion.
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

    
// Ejecuta NombreDe.
    private string NombreDe(string id) => id switch
    {
        "BANCO" => "el Banco",
        "-" => "nadie",
        _ => estado.Jugadores.Buscar(j => j.Id == id, out var jugador) ? jugador.Nombre : id
    };

    
// Ejecuta HistorialAnterior.
    public void HistorialAnterior()
    {
        lock (bloqueo)
        {
            cursorHistorial = Math.Max(0, cursorHistorial - 1);
            PublicarHistorial();
        }
    }

    
// Ejecuta HistorialSiguiente.
    public void HistorialSiguiente()
    {
        lock (bloqueo)
        {
            cursorHistorial = Math.Min(Math.Max(0, filtradas.Cantidad - 1), cursorHistorial + 1);
            PublicarHistorial();
        }
    }

    
// Ejecuta HistorialPrimero.
    public void HistorialPrimero()
    {
        lock (bloqueo)
        {
            cursorHistorial = 0;
            PublicarHistorial();
        }
    }

    
// Ejecuta HistorialUltimo.
    public void HistorialUltimo()
    {
        lock (bloqueo)
        {
            cursorHistorial = Math.Max(0, filtradas.Cantidad - 1);
            PublicarHistorial();
        }
    }

    
// Ejecuta Desconectar.
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
