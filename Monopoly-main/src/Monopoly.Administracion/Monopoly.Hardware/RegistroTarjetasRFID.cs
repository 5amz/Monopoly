#nullable enable
using System.Globalization;

namespace Monopoly.Hardware;


public sealed class RegistroTarjetasRFID
{
    private sealed class NodoTarjeta
    {
        public string Uid { get; }
        public string IdJugador { get; }
        public NodoTarjeta? Siguiente { get; set; }
// Crea el objeto.
        public NodoTarjeta(string uid, string idJugador) { Uid = uid; IdJugador = idJugador; }
    }

    private NodoTarjeta? _primera;

// Crea el objeto.
    public RegistroTarjetasRFID(bool incluirPredeterminadas = true)
    {
        if (!incluirPredeterminadas) return;
        Registrar("62384551", "J1");
        Registrar("A98F8656", "J2");
        Registrar("D03B9032", "J3");
        Registrar("2203D734", "J4");
    }

    
// Ejecuta Registrar.
    public void Registrar(string uid, string idJugador)
    {
        if (!IntentarNormalizarUid(uid, out string normalizado))
            throw new ArgumentException("UID hexadecimal inválido.", nameof(uid));
        if (string.IsNullOrWhiteSpace(idJugador))
            throw new ArgumentException("El jugador es obligatorio.", nameof(idJugador));
        for (NodoTarjeta? nodo = _primera; nodo is not null; nodo = nodo.Siguiente)
        {
            if (nodo.Uid != normalizado) continue;
            if (nodo.IdJugador == idJugador) return;
            throw new InvalidOperationException("Esta tarjeta ya pertenece a otro jugador.");
        }

        _primera = new NodoTarjeta(normalizado, idJugador) { Siguiente = _primera };
    }

// Ejecuta BuscarJugador.
    public ResultadoLecturaRFID BuscarJugador(string uid)
    {
        if (IntentarNormalizarUid(uid, out string normalizado))
            for (NodoTarjeta? nodo = _primera; nodo is not null; nodo = nodo.Siguiente)
                if (nodo.Uid == normalizado)
                    return new ResultadoLecturaRFID(true, nodo.IdJugador);

        return new ResultadoLecturaRFID(false, null);
    }

    
// Ejecuta IntentarNormalizarUid.
    public static bool IntentarNormalizarUid(string? uid, out string normalizado)
    {
        normalizado = string.Empty;
        if (uid is null || (uid.Length != 8 && uid.Length != 10 && uid.Length != 14 && uid.Length != 20))
            return false;
        foreach (char caracter in uid)
            if (!Uri.IsHexDigit(caracter)) return false;

        normalizado = uid.ToUpperInvariant();
        if (normalizado.Length == 10)
        {
            int bcc = 0;
            for (int i = 0; i < 8; i += 2)
                bcc ^= int.Parse(normalizado.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int recibido = int.Parse(normalizado.Substring(8, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (bcc != recibido) { normalizado = string.Empty; return false; }
            normalizado = normalizado.Substring(0, 8);
        }
        return true;
    }
}
