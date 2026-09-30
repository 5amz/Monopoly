using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Monopoly.Nucleo;

namespace Monopoly.Cliente;

internal sealed class AudioWindows : IAudioCiudad
{
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(string comando, StringBuilder? respuesta, int capacidad, IntPtr ventana);
    private readonly Action<string> informar;
    private readonly string alias = "ciudad_" + Guid.NewGuid().ToString("N");
    private bool abierto;
    // Guarda la función que recibe avisos de audio.
    public AudioWindows(Action<string> informar) => this.informar = informar;
    // Envía una orden al reproductor de Windows.
    private bool Comando(string texto) => mciSendString(texto, null, 0, IntPtr.Zero) == 0;
    // Reproduce el fragmento elegido de la canción.
    public bool Reproducir(string archivo, double inicio, double duracion)
    {
        Detener();
        string tipo = archivo.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ? "waveaudio" : "mpegvideo";
        abierto = Comando("open \"" + archivo + "\" type " + tipo + " alias " + alias);
        if (abierto && Comando("set " + alias + " time format milliseconds"))
        {
            var respuesta = new StringBuilder(64);
            int error = mciSendString("status " + alias + " length", respuesta, respuesta.Capacity, IntPtr.Zero);
            long desde = (long)(inicio * 1000);
            if (error == 0 && long.TryParse(respuesta.ToString(), out long largo))
            {
                if (desde >= largo)
                {
                    informar("Audio omitido: " + Path.GetFileName(archivo) + ". El inicio (" + inicio.ToString("0.###")
                        + " s) está fuera de la canción (" + (largo / 1000d).ToString("0.###") + " s).");
                    Detener();
                    return false;
                }
                long hasta = Math.Min(largo, desde + (long)(duracion * 1000));
                if (Comando("play " + alias + " from " + desde + " to " + hasta)) return true;
            }
        }
        informar("Audio omitido: " + Path.GetFileName(archivo) + ". Revise el formato y que el inicio esté dentro de la canción.");
        Detener();
        return false;
    }
    public bool Reproduciendo
    {
        get
        {
            if (!abierto) return false;
            var estado = new StringBuilder(64);
            return mciSendString("status " + alias + " mode", estado, estado.Capacity, IntPtr.Zero) == 0 && estado.ToString() == "playing";
        }
    }
    // Detiene el recurso en uso.
    public void Detener()
    {
        if (abierto) Comando("close " + alias);
        abierto = false;
    }
    // Libera los recursos al cerrar.
    public void Dispose() => Detener();
}

