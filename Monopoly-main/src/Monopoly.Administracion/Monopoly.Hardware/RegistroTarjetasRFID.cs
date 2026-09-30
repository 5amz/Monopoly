#nullable enable
using System.Globalization;

namespace Monopoly.Hardware;

/// <summary>Tabla de asociación propia mediante nodos; una tarjeta nunca contiene saldo.</summary>
public sealed class RegistroTarjetasRFID
{
    private sealed class NodoTarjeta
    {
        public string Uid { get; }
        public string IdJugador { get; }
        public NodoTarjeta? Siguiente { get; set; }
        public NodoTarjeta(string uid, string idJugador) { Uid = uid; IdJugador = idJugador; }
    }

    private NodoTarjeta? _primera;

    public RegistroTarjetasRFID(bool incluirPredeterminadas = true)
    {
        if (!incluirPredeterminadas) return;
        Registrar("62384551", "J1");
        Registrar("A98F8656", "J2");
        Registrar("D03B9032", "J3");
        Registrar("2203D734", "J4");
    }

    /// <summary>Configuración local del servidor; impide asignar una tarjeta a dos jugadores.</summary>
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

    public ResultadoLecturaRFID BuscarJugador(string uid)
    {
        if (IntentarNormalizarUid(uid, out string normalizado))
            for (NodoTarjeta? nodo = _primera; nodo is not null; nodo = nodo.Siguiente)
                if (nodo.Uid == normalizado)
                    return new ResultadoLecturaRFID(true, nodo.IdJugador);

        return new ResultadoLecturaRFID(false, null);
    }

    /// <summary>UID de 4/7/10 bytes. Compatibilidad temporal: cuatro bytes más BCC válido.</summary>
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
