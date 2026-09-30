using System;
using System.Text;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;
public sealed class EstadoLocal
{
    public ListaSimple<JugadorVista> Jugadores { get; private set; } = new ListaSimple<JugadorVista>().Congelar();
    public ListaCircularDoble<CasillaVista> Casillas { get; private set; } = new ListaCircularDoble<CasillaVista>().Congelar();
    public ListaSimple<TransaccionVista> Transacciones { get; private set; } = new ListaSimple<TransaccionVista>().Congelar();
    public CartaVisible? Carta { get; internal set; }
    public int Turno { get; private set; }
    public int MaxTurnos { get; private set; }
    public string IdJugadorActual { get; private set; } = "-";

    private readonly ColaCircular<string> log = new(50);
    // Lee el estado completo y reemplaza los datos anteriores al terminar.
    public void AplicarSnapshot(Mensaje mensaje)
    {
        AnalizadorMensajes.Validar(mensaje);
        ListaSimple<JugadorVista> jugadores = new();
        ListaCircularDoble<CasillaVista> casillas = new();
        foreach (string fila in mensaje.Obtener("jugadores").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            JugadorVista jugador = LeerJugador(fila);
            if (jugadores.Buscar(otro => otro.Id == jugador.Id, out _))
            {
                throw new FormatException("El estado tiene un jugador repetido.");
            }

            jugadores.Agregar(jugador);
        }

        foreach (string fila in mensaje.Obtener("casillas").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] campos = fila.Split(',');
            CasillaVista casilla = new(Numeros.Entero(campos[0]), campos[1], campos[2], Numeros.Decimal(campos[3]), Numeros.Decimal(campos[4]), campos[5]);
            // Los snapshots reemplazan nodos como antes; conservamos el recurso del mismo id.
            foreach (var anterior in Casillas)
                if (anterior.Id == casilla.Id && anterior.Tipo == casilla.Tipo)
                {
                    casilla.Musica = anterior.Musica;
                    casilla.ConfiguracionMusical = anterior.ConfiguracionMusical;
                    break;
                }
            casillas.Agregar(casilla);
        }

        if (casillas.Cantidad != 24 || jugadores.Cantidad > 4)
        {
            throw new FormatException("Este cliente usa un tablero de 24 casillas y hasta 4 jugadores.");
        }

        foreach (JugadorVista jugador in jugadores)
        {
            if (jugador.Posicion < 0 || jugador.Posicion >= casillas.Cantidad)
            {
                throw new FormatException("La posición del jugador está fuera del tablero.");
            }
        }

        int turno = mensaje.Entero("turno");
        int maximo = mensaje.Entero("maxTurnos");
        string actual = mensaje.Obtener("idJugadorActual");
        if (maximo < 1 || turno > maximo)
        {
            throw new FormatException("El número de turno no es válido.");
        }

        if (actual != "-" && !jugadores.Buscar(jugador => jugador.Id == actual, out _))
        {
            throw new FormatException("El jugador en turno no aparece en el estado.");
        }

        Jugadores = jugadores.Congelar();
        Casillas = casillas.Congelar();
        Turno = turno;
        MaxTurnos = maximo;
        IdJugadorActual = actual;
    }

    // Lee una fila del mensaje y crea los datos de un jugador.
    private JugadorVista LeerJugador(string fila)
    {
        string[] campos = fila.Split(',');
        ListaSimple<int> propiedades = new();
        if (campos[5] != "-")
        {
            foreach (string id in campos[5].Split('+'))
            {
                propiedades.Agregar(Numeros.Entero(id));
            }
        }

        return new JugadorVista(campos[0], campos[1], Numeros.Decimal(campos[2]), Numeros.Entero(campos[3]), campos[4] == "1", propiedades);
    }

    // Guarda las últimas cincuenta líneas del registro.
    public void RegistrarLog(string linea)
    {
        if (log.Cantidad == 50)
        {
            log.Desencolar(out _);
        }

        log.Encolar(linea);
    }

    // Junta las líneas del registro para mostrarlas en la vista.
    public string ObtenerLog()
    {
        StringBuilder texto = new();
        foreach (string linea in log.Instantanea())
        {
            texto.AppendLine(linea);
        }

        return texto.ToString();
    }

    // Agrega la transacción por orden de id y evita repetirla.
    public void AgregarTransaccion(TransaccionVista transaccion)
    {
        if (Transacciones.Buscar(anterior => anterior.Id == transaccion.Id, out _))
        {
            return;
        }

        ListaSimple<TransaccionVista> copia = new();
        bool agregada = false;
        foreach (TransaccionVista anterior in Transacciones)
        {
            if (!agregada && transaccion.Id < anterior.Id)
            {
                copia.Agregar(transaccion);
                agregada = true;
            }

            copia.Agregar(anterior);
        }

        if (!agregada)
        {
            copia.Agregar(transaccion);
        }

        Transacciones = copia.Congelar();
    }

    // Crea los datos de una transacción recibida como evento.
    public static TransaccionVista DesdeEvento(Mensaje mensaje)
    {
        return new TransaccionVista(mensaje.Entero("id"), mensaje.Entero("turno"), mensaje.Obtener("tipo"), mensaje.Obtener("origen"), mensaje.Obtener("destino"), mensaje.Dinero("monto"), mensaje.Obtener("descripcion"));
    }

    // Lee una respuesta del historial y guarda sus transacciones.
    public void AplicarHistorial(Mensaje mensaje)
    {
        AnalizadorMensajes.Validar(mensaje);
        ListaSimple<TransaccionVista> recibidas = new();
        foreach (string fila in mensaje.Obtener("transacciones").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] campos = fila.Split(',');
            recibidas.Agregar(new TransaccionVista(Numeros.Entero(campos[0]), Numeros.Entero(campos[1]), campos[2], campos[3], campos[4], Numeros.Decimal(campos[5]), campos[6]));
        }

        if (recibidas.Cantidad != mensaje.Entero("cantidad"))
        {
            throw new FormatException("La cantidad de transacciones no coincide.");
        }

        foreach (TransaccionVista transaccion in recibidas)
        {
            AgregarTransaccion(transaccion);
        }
    }
}
