using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Monopoly.Protocolo;
public static class ConstantesProtocolo
{
    public const int Puerto = 5000;
    public const int MaximoLinea = 1_048_576;
    public const int MaximoJugadores = 4;
    public const string Banco = "BANCO";
    public const string Errores = "FUERA_DE_TURNO SALDO_INSUFICIENTE PROPIEDAD_OCUPADA PROPIEDAD_PROPIA DADOS_YA_LANZADOS DADOS_NO_LANZADOS JUGADOR_NO_EXISTE PARTIDA_LLENA PARTIDA_NO_INICIADA ACCION_INVALIDA FORMATO_INVALIDO";
}

public static class AnalizadorMensajes
{
    // Comprueba el tamaño y los caracteres del nombre.
    public static bool NombreValido(string nombre)
    {
        return nombre.Trim() == nombre && nombre.Trim().Length > 0 && Regex.IsMatch(nombre, @"\A[\p{L}\p{Nd} -]{1,16}\z");
    }

    // Comprueba que el id tenga letras o números.
    public static bool IdValido(string id)
    {
        return Regex.IsMatch(id, @"\A[A-Za-z0-9]{1,8}\z");
    }

    // Separa la línea recibida y crea el mensaje.
    public static Mensaje Analizar(string linea)
    {
        if (linea.Length == 0 || linea.Length > ConstantesProtocolo.MaximoLinea || linea.Contains('\r') || linea.Contains('\n'))
        {
            throw new FormatException("Línea inválida.");
        }

        string[] partes = linea.Split('|');
        var tabla = new TablaCampos();
        for (int i = 1; i < partes.Length; i++)
        {
            int separador = partes[i].IndexOf('=');
            if (separador <= 0 || partes[i].IndexOf('=', separador + 1) >= 0)
            {
                throw new FormatException("Campo inválido.");
            }

            tabla.Agregar(partes[i][..separador], partes[i][(separador + 1)..]);
        }

        var m = new Mensaje(partes[0], tabla);
        Validar(m);
        return m;
    }

    // Revisa que el mensaje tenga los campos y valores correctos.
    public static void Validar(Mensaje m)
    {
        string esquema = m.Tipo switch
        {
            "CONECTAR" => m.ObtenerOpcional("token").Length == 0 ? "nombre" : "nombre token",
            "TIRAR_DADOS" or "NO_COMPRAR" or "TERMINAR_TURNO" or "CONSULTAR_ESTADO" or "DESCONECTAR" => "idJugador",
            "SIMULAR_RFID" => "idJugador uid",
            "COMPRAR_PROPIEDAD" => "idJugador idCasilla",
            "CONSULTAR_TRANSACCIONES" => "idJugador tipo jugador",
            "PING" or "PONG" => "",
            "OK" when m.Obtener("accion") == "CONECTAR" => "accion idJugador nombre totalJugadores token",
            "OK" when m.Obtener("accion") == "CONSULTAR_TRANSACCIONES" => "accion cantidad transacciones",
            "ERROR" => "accion codigo mensaje",
            "EVT_JUGADOR_CONECTADO" => "idJugador nombre totalJugadores",
            "EVT_PARTIDA_INICIADA" => "turno idJugadorActual maxTurnos",
            "EVT_TURNO" => "turno idJugadorActual",
            "EVT_DADOS" => "idJugador d1 d2 total origen",
            "EVT_RECORRIDO" => "idJugador desde hasta pasos salto",
            "EVT_MOVIMIENTO" => "idJugador desde hasta pasoPorInicio",
            "EVT_CASILLA" => "idJugador idCasilla tipo nombre precio alquiler idPropietario accionDisponible",
            "EVT_COMPRA" => "idJugador idCasilla monto saldoNuevo",
            "EVT_ALQUILER" => "idPagador idCobrador idCasilla monto",
            "EVT_CARTA" => "idJugador texto efecto valor",
            "EVT_TRANSACCION" => "id turno tipo origen destino monto descripcion",
            "EVT_RFID" => "idJugador estado uid",
            "EVT_PAGO_PENDIENTE" => "idJugador descripcion",
            "EVT_ELIMINADO" => "idJugador motivo",
            "EVT_FIN_PARTIDA" => "idGanador nombre patrimonio motivo",
            "EVT_ESTADO" => "turno idJugadorActual maxTurnos jugadores casillas estado dadosUsados compraPendiente pagoPendiente descripcionPago modo revision",
            _ => throw new FormatException("Tipo o acción de mensaje desconocido.")};
        int cantidad = 0;
        foreach (string clave in esquema.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            m.Obtener(clave);
            cantidad++;
        }

        if (m.Campos.Cantidad != cantidad)
        {
            throw new FormatException("Campos adicionales no permitidos.");
        }

        foreach (var c in m.Campos)
        {
            if (c.Valor.Contains('|') || c.Valor.Contains('=') || c.Valor.Contains('\n') || c.Valor.Contains('\r'))
            {
                throw new FormatException("Delimitador dentro de un valor.");
            }

            if (c.Clave is not ("jugadores" or "casillas" or "transacciones") && (c.Valor.Contains(';') || c.Valor.Contains(',') || c.Valor.Contains('+')))
            {
                throw new FormatException("Separador de lista en un valor atómico.");
            }

            if (c.Clave is "idJugador" or "idPagador" or "idCobrador" or "idGanador")
            {
                if (!IdValido(c.Valor) || c.Valor == "BANCO")
                {
                    throw new FormatException("Identidad inválida.");
                }
            }

            if (c.Clave is "idJugadorActual" or "idPropietario" or "pagoPendiente")
            {
                if (c.Valor != "-" && (!IdValido(c.Valor) || c.Valor == "BANCO"))
                {
                    throw new FormatException("Referencia inválida.");
                }
            }

            if (c.Clave is "turno" or "maxTurnos" or "totalJugadores" or "cantidad" or "idCasilla" or "desde" or "hasta" or "d1" or "d2" or "total" or "id" or "revision")
            {
                if (Numeros.Entero(c.Valor) < 0)
                {
                    throw new FormatException("Entero negativo.");
                }
            }

            if (c.Clave == "token" && !Regex.IsMatch(c.Valor, @"\A[A-Fa-f0-9]{64}\z"))
                throw new FormatException("Credencial de reconexión inválida.");

            if (c.Clave == "valor")
            {
                Numeros.Decimal(c.Valor);
            }

            if (c.Clave is "saldoNuevo" or "precio" or "alquiler" or "monto" or "patrimonio")
            {
                if (Numeros.Decimal(c.Valor) < 0)
                {
                    throw new FormatException("Monto negativo.");
                }
            }
        }

        foreach (var campo in m.Campos)
        {
            if (campo.Clave is "jugadores" or "casillas" or "transacciones")
            {
                ValidarLista(campo.Clave, campo.Valor);
            }
        }

        if (m.Tipo == "CONECTAR" && !NombreValido(m.Obtener("nombre")))
        {
            throw new FormatException("Nombre inválido: de 1 a 16 letras números espacios o guiones.");
        }

        if (m.Tipo == "ERROR" && !(" " + ConstantesProtocolo.Errores + " ").Contains(" " + m.Obtener("codigo") + " "))
        {
            throw new FormatException("Código de error desconocido.");
        }

        if (m.Tipo == "EVT_MOVIMIENTO")
        {
            m.Booleano("pasoPorInicio");
        }

        if (m.Tipo == "EVT_RECORRIDO")
        {
            int pasos = m.Entero("pasos");
            if (Math.Abs((long)pasos) > 240 || m.Entero("desde") >= 24 || m.Entero("hasta") >= 24)
            {
                throw new FormatException("Recorrido fuera del tablero.");
            }

            bool salto = m.Booleano("salto");
            if (!salto && ((m.Entero("desde") + pasos) % 24 + 24) % 24 != m.Entero("hasta"))
            {
                throw new FormatException("El recorrido no coincide con la llegada.");
            }
        }

        if (m.Tipo == "EVT_DADOS")
        {
            bool soloTotal = m.Entero("d1") == 0 && m.Entero("d2") == 0;
            bool carasValidas = m.Entero("d1") >= 1 && m.Entero("d1") <= 6 && m.Entero("d2") >= 1 && m.Entero("d2") <= 6 && m.Entero("total") == m.Entero("d1") + m.Entero("d2");
            if ((!soloTotal && !carasValidas) || m.Entero("total") < 2 || m.Entero("total") > 12)
            {
                throw new FormatException("Dados invalidos.");
            }
        }

        if (m.Tipo == "EVT_ESTADO")
        {
            m.Booleano("dadosUsados");
            m.Booleano("compraPendiente");
            if (m.Obtener("estado") is not ("ESPERANDO" or "EN_CURSO" or "FINALIZADA") ||
                m.Obtener("modo") is not ("HARDWARE" or "SIMULADOR"))
                throw new FormatException("Estado de partida inválido.");
        }

        if (m.Tipo == "CONSULTAR_TRANSACCIONES" &&
            m.Obtener("jugador") != "TODOS" && !IdValido(m.Obtener("jugador")))
            throw new FormatException("Filtro de jugador inválido.");

        if (m.Tipo == "EVT_CASILLA" && m.Obtener("tipo")is not ("PROPIEDAD" or "EVENTO" or "ESPECIAL"))
        {
            throw new FormatException("Tipo de casilla inválido.");
        }
    }

    // Comprueba los datos de cada fila recibida.
    private static void ValidarLista(string clave, string texto)
    {
        if (texto.Length == 0)
        {
            return;
        }

        foreach (string fila in texto.Split(';'))
        {
            string[] p = fila.Split(',');
            if (p.Length != (clave == "transacciones" ? 7 : 6))
            {
                throw new FormatException("Número de subcampos inválido en " + clave);
            }

            for (int i = 0; i < p.Length; i++)
            {
                if (!(clave == "jugadores" && i == 5) && (p[i].Contains('+') || p[i].Length == 0))
                {
                    throw new FormatException("Subcampo atómico inválido.");
                }
            }

            if (clave == "jugadores")
            {
                if (!IdValido(p[0]) || p[0] == "BANCO" || !NombreValido(p[1]) || Numeros.Decimal(p[2]) < 0 || Numeros.Entero(p[3]) < 0 || p[4] is not ("0" or "1"))
                {
                    throw new FormatException("Jugador inválido.");
                }

                if (p[5] != "-")
                {
                    foreach (string id in p[5].Split('+'))
                    {
                        if (Numeros.Entero(id) < 1)
                        {
                            throw new FormatException("Id de propiedad inválido.");
                        }
                    }
                }
            }
            else if (clave == "casillas")
            {
                if (Numeros.Entero(p[0]) < 1 || p[1] is not ("PROPIEDAD" or "EVENTO" or "ESPECIAL") || Numeros.Decimal(p[3]) < 0 || Numeros.Decimal(p[4]) < 0 || p[5] != "-" && (!IdValido(p[5]) || p[5] == "BANCO"))
                {
                    throw new FormatException("Casilla inválida.");
                }
            }
            else
            {
                if (Numeros.Entero(p[0]) < 1 || Numeros.Entero(p[1]) < 0 || !IdValido(p[3]) || !IdValido(p[4]) || Numeros.Decimal(p[5]) < 0)
                {
                    throw new FormatException("Transacción inválida.");
                }
            }
        }
    }

    // Lee hasta el salto de línea y rechaza mensajes incompletos o demasiado grandes.
    public static string? LeerLinea(StreamReader lector)
    {
        var texto = new StringBuilder();
        while (true)
        {
            int c = lector.Read();
            if (c < 0)
            {
                if (texto.Length == 0)
                {
                    return null;
                }

                throw new FormatException("Conexión cerrada a mitad de un mensaje.");
            }

            if (c == '\n')
            {
                return texto.ToString();
            }

            if (texto.Length >= ConstantesProtocolo.MaximoLinea)
            {
                throw new FormatException("Mensaje demasiado grande.");
            }

            texto.Append((char)c);
        }
    }
}

public interface IServidorEmbebido
{
    event Action? ServidorListo;
    event Action<string>? ServidorFallo;
    // Inicia el servidor sin esperar una entrada de consola.
    void Iniciar(int puerto);
    // Detiene el servidor y cierra sus conexiones.
    void Detener();
}
