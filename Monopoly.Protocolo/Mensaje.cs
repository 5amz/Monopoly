using System;
using System.Globalization;
using System.Text;

namespace Monopoly.Protocolo;
public sealed class Campo
{
    public string Clave { get; }
    public string Valor { get; }

    // Guarda el nombre y el valor de un campo del mensaje.
    public Campo(string Clave, string Valor)
    {
        this.Clave = Clave;
        this.Valor = Valor;
    }
}

public sealed class TablaCampos
{
    private readonly ListaSimple<Campo> campos = new();
    // Agrega un elemento al final de la estructura.
    internal void Agregar(string clave, string valor)
    {
        if (campos.Buscar(c => c.Clave == clave, out _))
        {
            throw new FormatException("Campo duplicado: " + clave);
        }

        campos.Agregar(new Campo(clave, valor));
    }

    internal ListaSimple<Campo> Elementos => campos;

    // Busca el campo y avisa si existe.
    public bool IntentarObtener(string clave, out string valor)
    {
        if (campos.Buscar(c => c.Clave == clave, out var campo))
        {
            valor = campo.Valor;
            return true;
        }

        valor = "";
        return false;
    }
}

public sealed class Mensaje
{
    private readonly TablaCampos campos;
    public string Tipo { get; }

    // Guarda el tipo y los campos del mensaje.
    internal Mensaje(string tipo, TablaCampos tabla)
    {
        Tipo = tipo;
        campos = tabla;
    }

    // Crea un mensaje con el tipo indicado.
    public static Mensaje Crear(string tipo)
    {
        return new(tipo, new TablaCampos());
    }

    // Crea una copia del mensaje con el campo indicado.
    public Mensaje Con(string clave, string valor)
    {
        var copia = new TablaCampos();
        foreach (var c in campos.Elementos)
        {
            copia.Agregar(c.Clave, c.Valor);
        }

        copia.Agregar(clave, valor);
        return new Mensaje(Tipo, copia);
    }

    // Crea una copia del mensaje con el campo indicado.
    public Mensaje Con(string clave, int valor)
    {
        return Con(clave, valor.ToString(CultureInfo.InvariantCulture));
    }

    // Crea una copia del mensaje con el campo indicado.
    public Mensaje Con(string clave, decimal valor)
    {
        return Con(clave, valor.ToString(CultureInfo.InvariantCulture));
    }

    // Crea una copia del mensaje con el campo indicado.
    public Mensaje Con(string clave, bool valor)
    {
        return Con(clave, valor ? "true" : "false");
    }

    // Busca y devuelve el dato solicitado.
    public string Obtener(string clave)
    {
        return campos.IntentarObtener(clave, out var valor) ? valor : throw new FormatException("Falta el campo " + clave);
    }

    public string ObtenerOpcional(string clave, string defecto = "")
    {
        return campos.IntentarObtener(clave, out var valor) ? valor : defecto;
    }

    // Convierte el texto a un número entero.
    public int Entero(string clave)
    {
        return Numeros.Entero(Obtener(clave));
    }

    // Lee un monto con punto decimal.
    public decimal Dinero(string clave)
    {
        return Numeros.Decimal(Obtener(clave));
    }

    // Lee un valor true o false del mensaje.
    public bool Booleano(string clave)
    {
        return Obtener(clave) switch
        {
            "true" => true,
            "false" => false,
            _ => throw new FormatException("Booleano inválido.")};
    }

    internal ListaSimple<Campo> Campos => campos.Elementos;

    // Junta el tipo y los campos para enviar el mensaje.
    public override string ToString()
    {
        AnalizadorMensajes.Validar(this);
        var s = new StringBuilder(Tipo);
        foreach (var c in campos.Elementos)
        {
            s.Append('|').Append(c.Clave).Append('=').Append(c.Valor);
        }

        return s.ToString();
    }
}

public static class Numeros
{
    // Convierte el texto a un número entero.
    public static int Entero(string texto)
    {
        if (!int.TryParse(texto, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int n))
        {
            throw new FormatException("Entero inválido.");
        }

        return n;
    }

    // Convierte el texto a un número decimal con punto.
    public static decimal Decimal(string texto)
    {
        if (!decimal.TryParse(texto, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal n))
        {
            throw new FormatException("Monto inválido.");
        }

        return n;
    }
}

public static class ConstructorMensajes
{
    // Crea la petición con el id del jugador.
    public static Mensaje Accion(string tipo, string id)
    {
        return Mensaje.Crear(tipo).Con("idJugador", id);
    }

    // Envía o prepara un mensaje que explica el error.
    public static Mensaje Error(string accion, string codigo, string texto)
    {
        return Mensaje.Crear("ERROR").Con("accion", accion).Con("codigo", codigo).Con("mensaje", TextoSeguro(texto));
    }

    // Quita del texto los separadores que usa el protocolo.
    public static string TextoSeguro(string texto)
    {
        var s = new StringBuilder();
        foreach (char c in texto)
        {
            s.Append("|;,+=\r\n".Contains(c) ? ' ' : c);
        }

        return s.ToString();
    }
}
