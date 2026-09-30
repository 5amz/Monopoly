using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Monopoly.Administracion;
using Monopoly.Hardware;
using Monopoly.Protocolo;

namespace Monopoly.Integracion;


public sealed class ServidorIntegrado : IServidorEmbebido, IDisposable
{
    private readonly object bloqueo = new();
    private readonly ConfiguracionPartida configuracion;
    private readonly RegistroTarjetasRFID tarjetas;
    private readonly ListaSimple<Sesion> sesiones = new();
    private readonly ListaSimple<JugadorConectado> jugadores = new();
    private readonly ListaSimple<Mensaje> cartasAplicadas = new();
    private readonly ServidorJuego juego;
    private readonly Tablero tablero;
    private readonly ColaTurnos turnos;
    private readonly CoordinadorPartidaTablero coordinador;
    private readonly ProveedorDadosFisico entradaSerial = new();
    private readonly ProveedorDadosObservado dados;
    private ConexionHardware hardware;
    private TcpListener listener;
    private volatile bool activo;
    private bool utilizado;
    private bool iniciada;
    private bool finalizada;
    private long ultimaTransaccion;
    private int revision;
    private string autorizadoRFID = "";
    private Mensaje ultimosDados;
    private Mensaje fin;
    private Mensaje ultimaCartaVisible;
    private int turnoCartaVisible;
    private string Actual => iniciada && !finalizada ? turnos.ObtenerJugadorActual()?.IdJugador ?? "-" : "-";
    private int Turno => iniciada ? Math.Min(coordinador.NumeroTurnoActual, configuracion.MaxTurnos) : 0;

    public int PuertoEscucha { get; private set; }
    public string UltimaExportacion { get; private set; }
    public event Action ServidorListo;
    public event Action<string> ServidorFallo;

// Crea el objeto.
    public ServidorIntegrado(ConfiguracionPartida opciones = null, IProveedorDados proveedorDados = null,
        RegistroTarjetasRFID registroTarjetas = null)
    {
        configuracion = (opciones ?? new ConfiguracionPartida()).Copiar();
        tarjetas = registroTarjetas ?? configuracion.CrearRegistroTarjetas();
        juego = new ServidorJuego(saldoInicialJugadores: configuracion.SaldoInicial);
        tablero = new ConfiguradorTablero().CrearTablero(configuracion.PremioInicio, configuracion.Impuesto, configuracion.ImpuestoEspecial);
        turnos = new ColaTurnos();
        IProveedorDados origen = proveedorDados ?? (configuracion.UsarHardware ? entradaSerial : new ProveedorDadosSimulado());
        dados = new ProveedorDadosObservado(origen, entradaSerial);
        coordinador = new CoordinadorPartidaTablero(juego, tablero, turnos, proveedorDados: dados,
            mazoEventos: configuracion.CrearMazo());
        juego.ConfigurarModulos(coordinador, coordinador, coordinador, coordinador);
        coordinador.CartaAplicada += (idJugador, carta) => cartasAplicadas.Agregar(Mensaje.Crear("EVT_CARTA")
            .Con("idJugador", idJugador).Con("texto", Limpiar(carta.Descripcion)).Con("efecto", carta.Tipo).Con("valor", carta.Valor));
    }

// Ejecuta Iniciar.
    public void Iniciar(int puerto)
    {
        lock (bloqueo)
        {
            if (utilizado)
            {
                ServidorFallo?.Invoke("Cree una nueva instancia para iniciar otra partida.");
                return;
            }
            utilizado = true;
            try
            {
                listener = new TcpListener(IPAddress.Any, puerto);
                listener.Start();
                PuertoEscucha = ((IPEndPoint)listener.LocalEndpoint).Port;
                activo = true;
                if (configuracion.UsarHardware)
                {
                    hardware = new ConexionHardware(configuracion.PuertoSerial, RecibirEventoHardware, FalloHardware);
                    hardware.Iniciar();
                }
                new Thread(Aceptar) { IsBackground = true, Name = "Servidor integrado" }.Start();
            }
            catch (Exception e) when (e is SocketException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
            {
                activo = false;
                listener?.Stop();
                hardware?.Dispose();
                hardware = null;
                ServidorFallo?.Invoke("No se pudo iniciar: " + e.Message);
                return;
            }
        }
        ServidorListo?.Invoke();
    }

// Ejecuta Aceptar.
    private void Aceptar()
    {
        try
        {
            while (activo)
            {
                var tcp = listener.AcceptTcpClient();
                lock (bloqueo)
                {
                    if (!activo) { tcp.Dispose(); break; }
                    var sesion = new Sesion(tcp, Procesar, Desconectada);
                    sesiones.Agregar(sesion);
                    sesion.Iniciar();
                }
            }
        }
        catch (Exception e) when (e is SocketException or ObjectDisposedException or InvalidOperationException) { }
    }

// Ejecuta Procesar.
    private void Procesar(Sesion sesion, Mensaje mensaje)
    {
        lock (bloqueo)
        {
            if (!activo) return;
            if (mensaje.Tipo == "PING") { sesion.Enviar(Mensaje.Crear("PONG")); return; }
            if (mensaje.Tipo == "CONECTAR") { Conectar(sesion, mensaje); return; }
            if (mensaje.Tipo is not ("TIRAR_DADOS" or "COMPRAR_PROPIEDAD" or "NO_COMPRAR" or "TERMINAR_TURNO" or
                "CONSULTAR_ESTADO" or "CONSULTAR_TRANSACCIONES" or "DESCONECTAR" or "SIMULAR_RFID"))
            {
                Error(sesion, mensaje.Tipo, "ACCION_INVALIDA", "El cliente solo puede enviar solicitudes.");
                return;
            }
            JugadorConectado jugador = sesion.Jugador;
            if (jugador == null || jugador.Sesion != sesion || mensaje.Obtener("idJugador") != jugador.Id)
            {
                Error(sesion, mensaje.Tipo, "ACCION_INVALIDA", "La identidad no corresponde a esta conexion.");
                return;
            }
            if (mensaje.Tipo == "CONSULTAR_ESTADO") { Contexto(sesion); return; }
            if (mensaje.Tipo == "CONSULTAR_TRANSACCIONES") { ConsultarHistorial(sesion, mensaje); return; }
            
            if (mensaje.Tipo == "DESCONECTAR") { sesion.Cerrar(); return; }
            if (!iniciada)
            {
                Error(sesion, mensaje.Tipo, "PARTIDA_NO_INICIADA", "Se necesitan cuatro jugadores conectados.");
                return;
            }
            if (finalizada || !jugador.Oficial.EstaActivo)
            {
                Error(sesion, mensaje.Tipo, "ACCION_INVALIDA", "La partida termino o el jugador fue eliminado.");
                return;
            }
            
            
            
            
            if (mensaje.Tipo == "SIMULAR_RFID") { SimularRfid(sesion, mensaje); return; }
            if (Actual != jugador.Id)
            {
                Error(sesion, mensaje.Tipo, "FUERA_DE_TURNO", "No es tu turno.");
                return;
            }
            switch (mensaje.Tipo)
            {
                case "TIRAR_DADOS":
                    if (coordinador.DadosUsadosEnTurno)
                        Error(sesion, mensaje.Tipo, "DADOS_YA_LANZADOS", "Ya lanzaste los dados.");
                    else if (configuracion.UsarHardware)
                        Error(sesion, mensaje.Tipo, "ACCION_INVALIDA", "Pasa tu tarjeta y pulsa el boton del Pico.");
                    else if (autorizadoRFID != jugador.Id)
                        Error(sesion, mensaje.Tipo, "ACCION_INVALIDA", "Acerque su tarjeta RFID antes de tirar los dados.");
                    else
                        TirarDados(jugador);
                    break;
                case "COMPRAR_PROPIEDAD":
                    if (tablero.ObtenerCasilla(jugador.Oficial.PosicionActual)?.Id != mensaje.Entero("idCasilla"))
                        Error(sesion, mensaje.Tipo, "ACCION_INVALIDA", "La casilla no coincide con la posicion oficial.");
                    else
                        AplicarAccion(sesion, mensaje.Tipo, coordinador.ComprarPropiedad(jugador.Id));
                    break;
                case "NO_COMPRAR":
                    AplicarAccion(sesion, mensaje.Tipo, coordinador.NoComprarPropiedad(jugador.Id));
                    break;
                case "TERMINAR_TURNO":
                    AplicarAccion(sesion, mensaje.Tipo, coordinador.TerminarTurno(jugador.Id));
                    break;
            }
        }
    }

// Ejecuta Conectar.
    private void Conectar(Sesion sesion, Mensaje mensaje)
    {
        string nombre = mensaje.Obtener("nombre");
        string token = mensaje.ObtenerOpcional("token");
        if (sesion.Jugador != null)
        {
            Error(sesion, "CONECTAR", "ACCION_INVALIDA", "Esta conexion ya tiene jugador.");
            return;
        }
        bool nuevo = !jugadores.Buscar(j => j.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase), out var jugador);
        if (nuevo)
        {
            if (token.Length > 0)
            {
                Error(sesion, "CONECTAR", "ACCION_INVALIDA", "La credencial no corresponde a un jugador registrado.");
                return;
            }
            if (iniciada || jugadores.Cantidad == 4)
            {
                Error(sesion, "CONECTAR", "PARTIDA_LLENA", "No hay plazas libres.");
                return;
            }
            string id = "J" + (jugadores.Cantidad + 1);
            ResultadoOperacion registro = juego.RegistrarJugador(id, nombre);
            if (!registro.FueExitosa) { Error(sesion, "CONECTAR", "ACCION_INVALIDA", registro.Mensaje); return; }
            jugador = new JugadorConectado(juego.Banco.ConsultarJugador(id));
            jugadores.Agregar(jugador);
        }
        else if (token.Length == 0)
        {
            Error(sesion, "CONECTAR", "ACCION_INVALIDA", "El nombre " + jugador.Nombre + " ya esta registrado en esta partida. Elija un nombre diferente en cualquier computadora.");
            return;
        }
        else if ((jugador.Sesion != null && !jugador.Sesion.Cerrada) || !jugador.Oficial.EstaActivo ||
            token.Length != jugador.Token.Length || !CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(token), Encoding.ASCII.GetBytes(jugador.Token)))
        {
            Error(sesion, "CONECTAR", "ACCION_INVALIDA", "Identidad duplicada o credencial de reconexion incorrecta.");
            return;
        }
        jugador.Sesion = sesion;
        sesion.Jugador = jugador;
        sesion.Enviar(Mensaje.Crear("OK").Con("accion", "CONECTAR").Con("idJugador", jugador.Id)
            .Con("nombre", jugador.Nombre).Con("totalJugadores", jugadores.Cantidad).Con("token", jugador.Token));
        Difundir(Mensaje.Crear("EVT_JUGADOR_CONECTADO").Con("idJugador", jugador.Id)
            .Con("nombre", jugador.Nombre).Con("totalJugadores", jugadores.Cantidad));
        int conectados = 0;
        foreach (var j in jugadores)
            if (j.Sesion != null && !j.Sesion.Cerrada) conectados++;
        if (!iniciada && conectados == 4)
        {
            juego.MarcarPartidaIniciada();
            iniciada = true;
            Difundir(Inicio());
        }
        Contexto(sesion);
        PublicarEstado();
    }

// Ejecuta Inicio.
    private Mensaje Inicio() => Mensaje.Crear("EVT_PARTIDA_INICIADA").Con("turno", Turno)
        .Con("idJugadorActual", Actual).Con("maxTurnos", configuracion.MaxTurnos);

// Ejecuta Contexto.
    private void Contexto(Sesion sesion)
    {
        if (iniciada && !finalizada)
        {
            sesion.Enviar(Inicio());
            sesion.Enviar(Mensaje.Crear("EVT_TURNO").Con("turno", Turno).Con("idJugadorActual", Actual));
            if (coordinador.DadosUsadosEnTurno && ultimosDados != null) sesion.Enviar(ultimosDados);
            if (jugadores.Buscar(j => j.Id == Actual, out var jugador)) sesion.Enviar(Casilla(jugador));
            if (coordinador.HayPagoPendiente) sesion.Enviar(PagoPendiente());
            if (ultimaCartaVisible != null && turnoCartaVisible == Turno && ultimaCartaVisible.Obtener("idJugador") == Actual)
                sesion.Enviar(ultimaCartaVisible);
        }
        if (fin != null) sesion.Enviar(fin);
        sesion.Enviar(Snapshot());
    }

// Ejecuta TirarDados.
    private void TirarDados(JugadorConectado jugador)
    {
        cartasAplicadas.Limpiar();
        int desde = jugador.Oficial.PosicionActual;
        ResultadoAccionJuego resultado = coordinador.TirarDados(jugador.Id);
        ResultadoDados tirada = dados.Ultimo;
        if (tirada != null)
        {
            autorizadoRFID = "";
            ultimosDados = Mensaje.Crear("EVT_DADOS").Con("idJugador", jugador.Id).Con("d1", tirada.Dado1 ?? 0)
                .Con("d2", tirada.Dado2 ?? 0).Con("total", tirada.Total).Con("origen", configuracion.UsarHardware ? "HARDWARE" : "SIMULADOR");
            Difundir(ultimosDados);
            int hasta = jugador.Oficial.PosicionActual;
            Difundir(Mensaje.Crear("EVT_RECORRIDO").Con("idJugador", jugador.Id).Con("desde", desde).Con("hasta", hasta)
                .Con("pasos", tirada.Total).Con("salto", (desde + tirada.Total) % tablero.Cantidad != hasta));
        }
        foreach (var carta in cartasAplicadas)
        {
            ultimaCartaVisible = carta;
            turnoCartaVisible = Turno;
            Difundir(carta);
        }
        AplicarAccion(jugador.Sesion, "TIRAR_DADOS", resultado);
    }

// Ejecuta AplicarAccion.
    private void AplicarAccion(Sesion sesion, string accion, ResultadoAccionJuego resultado)
    {
        if (!resultado.FueExitosa)
            Error(sesion, accion, "ACCION_INVALIDA", resultado.Mensaje);
        PublicarCambios();
    }

// Ejecuta PagoPendiente.
    private Mensaje PagoPendiente() => Mensaje.Crear("EVT_PAGO_PENDIENTE")
        .Con("idJugador", coordinador.IdJugadorPagoPendiente).Con("descripcion", Limpiar(coordinador.DescripcionPagoPendiente));

// Ejecuta PublicarCambios.
    private void PublicarCambios()
    {
        juego.Banco.Historial.RecorrerAntiguaAReciente(t =>
        {
            if (t.Id <= ultimaTransaccion) return;
            Difundir(EventoTransaccion(t));
            ultimaTransaccion = t.Id;
            if (t.Tipo == TipoTransaccion.CompraPropiedad && jugadores.Buscar(j => j.Id == t.IdJugadorOrigen, out var comprador))
                Difundir(Mensaje.Crear("EVT_COMPRA").Con("idJugador", comprador.Id)
                    .Con("idCasilla", tablero.ObtenerCasilla(comprador.Oficial.PosicionActual).Id).Con("monto", t.Monto).Con("saldoNuevo", comprador.Oficial.Saldo));
            if (t.Tipo == TipoTransaccion.PagoAlquiler && jugadores.Buscar(j => j.Id == t.IdJugadorOrigen, out var pagador))
                Difundir(Mensaje.Crear("EVT_ALQUILER").Con("idPagador", pagador.Id).Con("idCobrador", t.IdJugadorDestino)
                    .Con("idCasilla", tablero.ObtenerCasilla(pagador.Oficial.PosicionActual).Id).Con("monto", t.Monto));
        });
        foreach (var jugador in jugadores)
        {
            if (!jugador.Oficial.EstaActivo && !jugador.EliminacionAvisada)
            {
                jugador.EliminacionAvisada = true;
                Difundir(Mensaje.Crear("EVT_ELIMINADO").Con("idJugador", jugador.Id).Con("motivo", "Abandono o insolvencia ante un pago obligatorio"));
            }
        }
        if (iniciada && juego.Banco.CantidadJugadoresActivos <= 1) Finalizar("ULTIMO_ACTIVO");
        else if (iniciada && !coordinador.HayPagoPendiente && coordinador.NumeroTurnoActual > configuracion.MaxTurnos)
            Finalizar("LIMITE_TURNOS");
        if (!coordinador.DadosUsadosEnTurno)
        {
            ultimosDados = null;
            autorizadoRFID = "";
            dados.Limpiar();
        }
        if (!finalizada && iniciada)
        {
            if (jugadores.Buscar(j => j.Id == Actual, out var actual)) Difundir(Casilla(actual));
            if (coordinador.HayPagoPendiente) Difundir(PagoPendiente());
            if (!coordinador.DadosUsadosEnTurno)
                Difundir(Mensaje.Crear("EVT_TURNO").Con("turno", Turno).Con("idJugadorActual", Actual));
        }
        PublicarEstado();
    }

// Ejecuta Finalizar.
    private void Finalizar(string motivo)
    {
        if (finalizada) return;
        JugadorTablero ganador = coordinador.ObtenerGanador();
        juego.MarcarPartidaFinalizada();
        finalizada = true;
        if (ganador != null)
        {
            fin = Mensaje.Crear("EVT_FIN_PARTIDA").Con("idGanador", ganador.IdJugador).Con("nombre", ganador.Nombre)
                .Con("patrimonio", coordinador.CalcularPatrimonio(ganador.IdJugador)).Con("motivo", motivo);
            Difundir(fin);
        }
        try { ExportarTransacciones(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            ServidorFallo?.Invoke("La partida termino pero fallo la exportacion TXT: " + e.Message);
        }
    }

    
// Ejecuta AbandonarJugador.
    public bool AbandonarJugador(string idJugador)
    {
        lock (bloqueo)
        {
            if (!iniciada || finalizada) return false;
            var resultado = juego.EliminarJugador(idJugador, "Abandono administrativo local");
            PublicarCambios();
            return resultado.FueExitosa;
        }
    }

    
// Ejecuta ExportarTransacciones.
    public string ExportarTransacciones(string ruta = null)
    {
        lock (bloqueo)
        {
            string destino = Path.GetFullPath(ruta ?? configuracion.RutaTransacciones);
            Directory.CreateDirectory(Path.GetDirectoryName(destino));
            juego.ExportarTransacciones(destino);
            UltimaExportacion = destino;
            return destino;
        }
    }

    


// Ejecuta SimularRfid.
    private void SimularRfid(Sesion sesion, Mensaje mensaje)
    {
        if (configuracion.UsarHardware)
        {
            Error(sesion, mensaje.Tipo, "ACCION_INVALIDA", "El simulador solo esta disponible sin hardware.");
            return;
        }
        if (!AnalizadorHardware.IntentarAnalizar("RFID|" + mensaje.Obtener("uid"), out var evento))
        {
            Error(sesion, mensaje.Tipo, "FORMATO_INVALIDO", "UID invalido.");
            return;
        }
        ProcesarEventoHardware(evento);
    }

    
// Ejecuta RecibirLineaSerial.
    public bool RecibirLineaSerial(string linea)
    {
        lock (bloqueo)
        {
            if (!AnalizadorHardware.IntentarAnalizar(linea, out var evento)) return false;
            return ProcesarEventoHardware(evento);
        }
    }

// Ejecuta RecibirEventoHardware.
    private void RecibirEventoHardware(EventoHardware evento)
    {
        lock (bloqueo) ProcesarEventoHardware(evento);
    }

// Ejecuta ProcesarEventoHardware.
    private bool ProcesarEventoHardware(EventoHardware evento)
    {
        if (!activo || !iniciada || finalizada) return false;
        if (evento.Tipo == TipoEventoHardware.RFID)
        {
            autorizadoRFID = "";
            ResultadoLecturaRFID lectura = tarjetas.BuscarJugador(evento.Valor);
            bool correcto = lectura.FueEncontrado && lectura.IdJugador == Actual;
            Difundir(Mensaje.Crear("EVT_RFID").Con("idJugador", Actual).Con("estado", correcto ? "VALIDO" : "INVALIDO").Con("uid", evento.Valor));
            if (!correcto) return false;
            if (coordinador.HayPagoPendiente)
            {
                var resultado = coordinador.ConfirmarPagoConRfid(lectura.IdJugador);
                if (!resultado.FueExitosa && jugadores.Buscar(j => j.Id == lectura.IdJugador, out var jugador))
                    Error(jugador.Sesion, "RFID", "ACCION_INVALIDA", resultado.Mensaje);
                PublicarCambios();
                return resultado.FueExitosa;
            }
            
            if (coordinador.DadosUsadosEnTurno) return false;
            autorizadoRFID = Actual;
            return true;
        }
        if (evento.Tipo == TipoEventoHardware.Dado && autorizadoRFID == Actual && !coordinador.DadosUsadosEnTurno &&
            !coordinador.HayPagoPendiente && jugadores.Buscar(j => j.Id == Actual, out var actual) && entradaSerial.Entregar(evento.ResultadoDados))
        {
            TirarDados(actual);
            return true;
        }
        return false;
    }

// Ejecuta FalloHardware.
    private void FalloHardware(string texto)
    {
        lock (bloqueo)
        {
            autorizadoRFID = "";
            entradaSerial.Descartar();
            if (activo) Difundir(ConstructorMensajes.Error("HARDWARE", "ACCION_INVALIDA", Limpiar(texto)));
        }
    }

// Ejecuta TipoCasilla.
    private static string TipoCasilla(Casilla casilla) => casilla is Propiedad ? "PROPIEDAD" : casilla is CasillaEvento ? "EVENTO" : "ESPECIAL";

// Ejecuta Casilla.
    private Mensaje Casilla(JugadorConectado jugador)
    {
        Casilla casilla = tablero.ObtenerCasilla(jugador.Oficial.PosicionActual);
        Propiedad propiedad = casilla as Propiedad;
        return Mensaje.Crear("EVT_CASILLA").Con("idJugador", jugador.Id).Con("idCasilla", casilla.Id).Con("tipo", TipoCasilla(casilla))
            .Con("nombre", Limpiar(casilla.Nombre)).Con("precio", propiedad?.Precio ?? 0).Con("alquiler", propiedad?.Alquiler ?? 0)
            .Con("idPropietario", propiedad?.Propietario?.IdJugador ?? "-")
            .Con("accionDisponible", jugador.Oficial.EstaActivo && Actual == jugador.Id && coordinador.HayCompraPendiente && !coordinador.HayPagoPendiente ? "COMPRAR" : "NINGUNA");
    }

// Ejecuta Snapshot.
    private Mensaje Snapshot()
    {
        var datosJugadores = new StringBuilder();
        var datosCasillas = new StringBuilder();
        foreach (var conexion in jugadores)
        {
            Jugador jugador = conexion.Oficial;
            if (datosJugadores.Length > 0) datosJugadores.Append(';');
            var propiedades = new StringBuilder();
            NodoCasilla nodo = tablero.Head;
            for (int i = 0; i < tablero.Cantidad; i++, nodo = nodo.Next)
            {
                if (nodo.Casilla is Propiedad propiedad && propiedad.Propietario?.IdJugador == jugador.Id)
                {
                    if (propiedades.Length > 0) propiedades.Append('+');
                    propiedades.Append(propiedad.Id);
                }
            }
            datosJugadores.Append(jugador.Id).Append(',').Append(jugador.Nombre).Append(',')
                .Append(jugador.Saldo.ToString(CultureInfo.InvariantCulture)).Append(',').Append(jugador.PosicionActual).Append(',')
                .Append(jugador.EstaActivo ? "1" : "0").Append(',').Append(propiedades.Length == 0 ? "-" : propiedades.ToString());
        }
        NodoCasilla actual = tablero.Head;
        for (int i = 0; i < tablero.Cantidad; i++, actual = actual.Next)
        {
            Casilla casilla = actual.Casilla;
            Propiedad propiedad = casilla as Propiedad;
            if (datosCasillas.Length > 0) datosCasillas.Append(';');
            datosCasillas.Append(casilla.Id).Append(',').Append(TipoCasilla(casilla)).Append(',').Append(Limpiar(casilla.Nombre)).Append(',')
                .Append((propiedad?.Precio ?? (casilla is CasillaEspecial especial ? (especial.Tipo == "Salida" ? tablero.PremioInicio : especial.Monto) : 0)).ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append((propiedad?.Alquiler ?? 0).ToString(CultureInfo.InvariantCulture)).Append(',').Append(propiedad?.Propietario?.IdJugador ?? "-");
        }
        return Mensaje.Crear("EVT_ESTADO").Con("turno", Turno).Con("idJugadorActual", Actual).Con("maxTurnos", configuracion.MaxTurnos)
            .Con("jugadores", datosJugadores.ToString()).Con("casillas", datosCasillas.ToString())
            .Con("estado", finalizada ? "FINALIZADA" : iniciada ? "EN_CURSO" : "ESPERANDO")
            .Con("dadosUsados", coordinador.DadosUsadosEnTurno).Con("compraPendiente", coordinador.HayCompraPendiente)
            .Con("pagoPendiente", coordinador.HayPagoPendiente ? coordinador.IdJugadorPagoPendiente : "-")
            .Con("descripcionPago", coordinador.HayPagoPendiente ? Limpiar(coordinador.DescripcionPagoPendiente) : "-")
            .Con("modo", configuracion.UsarHardware ? "HARDWARE" : "SIMULADOR").Con("revision", revision);
    }

// Ejecuta TipoTransaccionCliente.
    private static string TipoTransaccionCliente(TipoTransaccion tipo) => tipo switch
    {
        TipoTransaccion.CompraPropiedad => "COMPRA_PROPIEDAD",
        TipoTransaccion.PagoAlquiler => "PAGO_ALQUILER",
        TipoTransaccion.PremioPorPasarInicio => "PASO_INICIO",
        TipoTransaccion.GananciaPorEvento => "CARTA_PREMIO",
        TipoTransaccion.PerdidaPorEvento => "CARTA_COBRO",
        _ => tipo.ToString()
    };

// Ejecuta EventoTransaccion.
    private static Mensaje EventoTransaccion(Transaccion transaccion) => Mensaje.Crear("EVT_TRANSACCION")
        .Con("id", checked((int)transaccion.Id)).Con("turno", transaccion.NumeroTurno).Con("tipo", TipoTransaccionCliente(transaccion.Tipo))
        .Con("origen", transaccion.IdJugadorOrigen).Con("destino", transaccion.IdJugadorDestino).Con("monto", transaccion.Monto)
        .Con("descripcion", Limpiar(transaccion.Descripcion));

// Ejecuta ConsultarHistorial.
    private void ConsultarHistorial(Sesion sesion, Mensaje mensaje)
    {
        var texto = new StringBuilder();
        int cantidad = 0;
        juego.Banco.Historial.RecorrerAntiguaAReciente(t =>
        {
            string tipo = TipoTransaccionCliente(t.Tipo);
            string jugador = mensaje.Obtener("jugador");
            if (mensaje.Obtener("tipo") != "TODOS" && mensaje.Obtener("tipo") != tipo && mensaje.Obtener("tipo") != t.Tipo.ToString()) return;
            if (jugador != "TODOS" && jugador != t.IdJugadorOrigen && jugador != t.IdJugadorDestino) return;
            if (cantidad++ > 0) texto.Append(';');
            texto.Append(t.Id).Append(',').Append(t.NumeroTurno).Append(',').Append(tipo).Append(',').Append(t.IdJugadorOrigen).Append(',')
                .Append(t.IdJugadorDestino).Append(',').Append(t.Monto.ToString(CultureInfo.InvariantCulture)).Append(',').Append(Limpiar(t.Descripcion));
        });
        sesion.Enviar(Mensaje.Crear("OK").Con("accion", "CONSULTAR_TRANSACCIONES").Con("cantidad", cantidad).Con("transacciones", texto.ToString()));
    }

// Ejecuta Limpiar.
    private static string Limpiar(string texto) => ConstructorMensajes.TextoSeguro(texto ?? "").Trim();
// Ejecuta Error.
    private static void Error(Sesion sesion, string accion, string codigo, string texto) => sesion?.Enviar(ConstructorMensajes.Error(accion, codigo, Limpiar(texto)));
// Ejecuta PublicarEstado.
    private void PublicarEstado() { revision++; Difundir(Snapshot()); }

// Ejecuta Difundir.
    private void Difundir(Mensaje mensaje)
    {
        AnalizadorMensajes.Validar(mensaje);
        foreach (var jugador in jugadores) jugador.Sesion?.Enviar(mensaje);
    }

// Ejecuta Desconectada.
    private void Desconectada(Sesion sesion)
    {
        lock (bloqueo)
        {
            sesiones.Quitar(s => s == sesion);
            if (sesion.Jugador != null && sesion.Jugador.Sesion == sesion)
            {
                sesion.Jugador.Sesion = null;
                if (activo) PublicarEstado();
            }
        }
    }

// Ejecuta Detener.
    public void Detener()
    {
        var pendientes = new ListaSimple<Sesion>();
        ConexionHardware cerrarHardware;
        lock (bloqueo)
        {
            if (!activo) return;
            activo = false;
            listener.Stop();
            cerrarHardware = hardware;
            hardware = null;
            foreach (var sesion in sesiones)
            {
                pendientes.Agregar(sesion);
                sesion.FinalizarConAviso(ConstructorMensajes.Error("DESCONECTAR", "ACCION_INVALIDA", "El organizador cerro el servidor."));
            }
        }
        cerrarHardware?.Dispose();
        foreach (var sesion in pendientes) sesion.EsperarCierre();
    }

// Ejecuta Dispose.
    public void Dispose() => Detener();
}
