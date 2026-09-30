using Monopoly.Hardware;

namespace Monopoly.Integracion;

public sealed class TarjetaConfigurada
{
    public string Uid { get; set; } = "";
    public string IdJugador { get; set; } = "";
}

/// <summary>Decisiones configurables del equipo; estos montos no son requisitos del PDF.</summary>
public sealed class ConfiguracionPartida
{
    public decimal SaldoInicial { get; set; } = 1500000;
    public int MaxTurnos { get; set; } = 60;
    public decimal PremioInicio { get; set; } = 200000;
    public decimal Impuesto { get; set; } = 100000;
    public decimal ImpuestoEspecial { get; set; } = 150000;
    public int PremioEvento { get; set; } = 100000;
    public int CobroEvento { get; set; } = 50000;
    public int AvanceEvento { get; set; } = 3;
    public int RetrocesoEvento { get; set; } = 2;
    public int DestinoEvento { get; set; } = 0;
    public bool UsarHardware { get; set; } = true;
    public string PuertoSerial { get; set; } = "COM3";
    public string RutaTransacciones { get; set; } = "transacciones-partida.txt";
    public TarjetaConfigurada[] Tarjetas { get; set; }

    internal ConfiguracionPartida Copiar()
    {
        Validar();
        var copia = (ConfiguracionPartida)MemberwiseClone();
        if (Tarjetas != null)
        {
            copia.Tarjetas = new TarjetaConfigurada[Tarjetas.Length];
            for (int i = 0; i < Tarjetas.Length; i++)
                copia.Tarjetas[i] = new TarjetaConfigurada { Uid = Tarjetas[i].Uid, IdJugador = Tarjetas[i].IdJugador };
        }
        return copia;
    }

    public void Validar()
    {
        if (MaxTurnos < 1 || SaldoInicial < 0 || PremioInicio < 0 || Impuesto < 0 || ImpuestoEspecial < 0 ||
            PremioEvento < 0 || CobroEvento < 0 || AvanceEvento < 0 || RetrocesoEvento < 0 ||
            AvanceEvento > 240 || RetrocesoEvento > 240 || DestinoEvento < 0 || DestinoEvento >= 24)
            throw new ArgumentException("Revise los montos, turnos y movimientos configurados.");
        if (string.IsNullOrWhiteSpace(RutaTransacciones) || (UsarHardware && string.IsNullOrWhiteSpace(PuertoSerial)))
            throw new ArgumentException("La ruta TXT y el puerto del hardware son obligatorios.");
        if (Tarjetas != null)
            foreach (var tarjeta in Tarjetas)
                if (tarjeta == null || !RegistroTarjetasRFID.IntentarNormalizarUid(tarjeta.Uid, out _) ||
                    tarjeta.IdJugador is not ("J1" or "J2" or "J3" or "J4"))
                    throw new ArgumentException("Cada tarjeta debe tener UID válido e IdJugador J1 a J4.");
    }

    internal RegistroTarjetasRFID CrearRegistroTarjetas()
    {
        var registro = new RegistroTarjetasRFID(incluirPredeterminadas: Tarjetas == null);
        if (Tarjetas != null)
            foreach (var tarjeta in Tarjetas) registro.Registrar(tarjeta.Uid, tarjeta.IdJugador);
        return registro;
    }

    public MazoEventos CrearMazo()
    {
        var mazo = new MazoEventos();
        mazo.AgregarCarta(new CartaEvento(1, "Tu concierto agoto las entradas", "GanarDinero", PremioEvento));
        mazo.AgregarCarta(new CartaEvento(2, "Toca pagar la reparacion del amplificador", "PerderDinero", CobroEvento));
        mazo.AgregarCarta(new CartaEvento(3, "Toma la carretera hacia las montanas", "Avanzar", AvanceEvento));
        mazo.AgregarCarta(new CartaEvento(4, "Vuelve por el equipo olvidado", "Retroceder", RetrocesoEvento));
        mazo.AgregarCarta(new CartaEvento(5, "Tu presentacion fue aplazada", "PerderTurno", 0));
        mazo.AgregarCarta(new CartaEvento(6, "El bus te lleva a la entrada de la ciudad", "IrACasilla", DestinoEvento));
        return mazo;
    }
}
