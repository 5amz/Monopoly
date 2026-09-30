using System;
using System.Globalization;
using System.Text;

namespace Monopoly.Protocolo;
public sealed class Campo
{
    public string Clave { get; }
    public string Valor { get; }

    
// Crea el objeto.
    public Campo(string Clave, string Valor)
    {
        this.Clave = Clave;
        this.Valor = Valor;
    }
}

public sealed class TablaCampos
{
    private readonly ListaSimple<Campo> campos = new();
    
// Ejecuta Agregar.
    internal void Agregar(string clave, string valor)
    {
        if (campos.Buscar(c => c.Clave == clave, out _))
        {
            throw new FormatException("Campo duplicado: " + clave);
        }

        campos.Agregar(new Campo(clave, valor));
    }

    internal ListaSimple<Campo> Elementos => campos;

    
// Ejecuta IntentarObtener.
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

    
// Crea el objeto.
    internal Mensaje(string tipo, TablaCampos tabla)
    {
        Tipo = tipo;
        campos = tabla;
    }

    
// Ejecuta Crear.
    public static Mensaje Crear(string tipo)
    {
        return new(tipo, new TablaCampos());
    }

    
// Ejecuta Con.
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

    
// Ejecuta Con.
    public Mensaje Con(string clave, int valor)
    {
        return Con(clave, valor.ToString(CultureInfo.InvariantCulture));
    }

    
// Ejecuta Con.
    public Mensaje Con(string clave, decimal valor)
    {
        return Con(clave, valor.ToString(CultureInfo.InvariantCulture));
    }

    
// Ejecuta Con.
    public Mensaje Con(string clave, bool valor)
    {
        return Con(clave, valor ? "true" : "false");
    }

    
// Ejecuta Obtener.
    public string Obtener(string clave)
    {
        return campos.IntentarObtener(clave, out var valor) ? valor : throw new FormatException("Falta el campo " + clave);
    }

// Ejecuta ObtenerOpcional.
    public string ObtenerOpcional(string clave, string defecto = "")
    {
        return campos.IntentarObtener(clave, out var valor) ? valor : defecto;
    }

    
// Ejecuta Entero.
    public int Entero(string clave)
    {
        return Numeros.Entero(Obtener(clave));
    }

    
// Ejecuta Dinero.
    public decimal Dinero(string clave)
    {
        return Numeros.Decimal(Obtener(clave));
    }

    
// Ejecuta Booleano.
    public bool Booleano(string clave)
    {
        return Obtener(clave) switch
        {
            "true" => true,
            "false" => false,
            _ => throw new FormatException("Booleano inválido.")};
    }

    internal ListaSimple<Campo> Campos => campos.Elementos;

    
// Ejecuta ToString.
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
    
// Ejecuta Entero.
    public static int Entero(string texto)
    {
        if (!int.TryParse(texto, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int n))
        {
            throw new FormatException("Entero inválido.");
        }

        return n;
    }

    
// Ejecuta Decimal.
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
    
// Ejecuta Accion.
    public static Mensaje Accion(string tipo, string id)
    {
        return Mensaje.Crear(tipo).Con("idJugador", id);
    }

    
// Ejecuta Error.
    public static Mensaje Error(string accion, string codigo, string texto)
    {
        return Mensaje.Crear("ERROR").Con("accion", accion).Con("codigo", codigo).Con("mensaje", TextoSeguro(texto));
    }

    
// Ejecuta TextoSeguro.
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
