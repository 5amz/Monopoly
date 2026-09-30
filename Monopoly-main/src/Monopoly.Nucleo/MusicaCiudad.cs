using System;
using System.IO;

namespace Monopoly.Nucleo;

public interface IAudioCiudad : IDisposable
{
    bool Reproducir(string archivo, double inicio, double duracion);
    bool Reproduciendo { get; }
    void Detener();
}

// Reproduce el fragmento guardado en la casilla/carta original; no mantiene una lista de pistas.
public sealed class MusicaCiudad : IDisposable
{
    private readonly string carpeta;
    private readonly ConfiguracionMusica configuracion;
    private readonly IAudioCiudad audio;
    private readonly Func<long> ahora;
    private readonly Action<string> informar;
    private readonly FragmentoMusical? fondo, victoria, derrota;
    private long hasta;
    private bool fondoSonando;
    private bool enPartida;
    public bool Silenciado { get; private set; }
    public bool FondoActivado { get; private set; }
    public double MilisegundosPorCasilla => configuracion.SegundosPorCasilla * 1000;
    public MusicaCiudad(string carpeta, Action<string> informar, IAudioCiudad audio, Func<long>? ahora = null)
    {
        this.carpeta = carpeta;
        this.informar = informar;
        configuracion = ConfiguracionMusica.Cargar(carpeta, informar);
        FondoActivado = configuracion.FondoActivado;
        fondo = configuracion.Obtener("fondo", 180);
        victoria = configuracion.Obtener("victoria", 15);
        derrota = configuracion.Obtener("derrota", 8);
        this.audio = audio;
        this.ahora = ahora ?? (() => Environment.TickCount64);
    }
    public void Iniciar() { enPartida = true; Actualizar(); }
    public void Silenciar(bool valor)
    {
        Silenciado = valor;
        if (valor) { audio.Detener(); hasta = 0; fondoSonando = false; }
        else Actualizar();
    }
    public void ActivarFondo(bool valor)
    {
        FondoActivado = valor;
        if (!valor && fondoSonando) { audio.Detener(); hasta = 0; fondoSonando = false; }
        Actualizar();
    }
    public void Preparar(EstadoLocal estado)
    {
        foreach (var casilla in estado.Casillas) configuracion.Aplicar(casilla);
        if (estado.Carta is not null) configuracion.Aplicar(estado.Carta);
    }
    public void Casilla(CasillaVista casilla)
    {
        configuracion.Aplicar(casilla);
        if (Silenciado) return;
        audio.Detener(); hasta = 0; fondoSonando = false;
        Reproducir(casilla.Imagen, casilla.Musica);
    }
    public void Carta(CartaVisible carta)
    {
        configuracion.Aplicar(carta);
        if (Silenciado) return;
        audio.Detener(); hasta = 0; fondoSonando = false;
        Reproducir(carta.Imagen, carta.Musica);
    }
    public void Evento(string nombre)
    {
        if (Silenciado) return;
        audio.Detener(); hasta = 0; fondoSonando = false;
        Reproducir(nombre, nombre == "victoria" ? victoria : nombre == "derrota" ? derrota : null);
    }
    private bool Reproducir(string clave, FragmentoMusical? pista)
    {
        if (pista is null || pista.Omitida || string.IsNullOrWhiteSpace(pista.Archivo)) return false;
        string archivo = pista.Archivo;
        if (archivo != Path.GetFileName(archivo) || archivo.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            !(archivo.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || archivo.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) ||
            !double.IsFinite(pista.InicioSegundos) || !double.IsFinite(pista.DuracionSegundos) ||
            pista.InicioSegundos < 0 || pista.InicioSegundos > 86400 || pista.DuracionSegundos < .05 || pista.DuracionSegundos > 2147483)
        {
            informar("Configuración de música inválida: " + clave + ". Se omite esa pista.");
            pista.Omitida = true; return false;
        }
        string ruta = Path.Combine(carpeta, archivo);
        if (!File.Exists(ruta))
        {
            informar("Falta música: " + archivo); pista.Omitida = true; return false;
        }
        if (!audio.Reproducir(ruta, pista.InicioSegundos, pista.DuracionSegundos)) { pista.Omitida = true; return false; }
        hasta = ahora() + (long)(pista.DuracionSegundos * 1000);
        fondoSonando = clave == "fondo";
        return true;
    }
    public void Actualizar()
    {
        if (!enPartida || Silenciado) return;
        if (hasta > ahora() && audio.Reproduciendo) return;
        audio.Detener(); hasta = 0; fondoSonando = false;
        if (FondoActivado) Reproducir("fondo", fondo);
    }
    public void Detener() { enPartida = false; hasta = 0; fondoSonando = false; audio.Detener(); }
    public void Dispose() { Detener(); audio.Dispose(); configuracion.Dispose(); }
}
