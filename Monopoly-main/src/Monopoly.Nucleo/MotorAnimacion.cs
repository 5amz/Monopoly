using System;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;
public sealed class Animacion
{
    public string Id { get; }
    public int Desde { get; }
    public int Pasos { get; }
    public double Transcurrido { get; set; }
    public EfectoMultimedia? Efecto { get; }
    public DefinicionEfecto? Presentacion { get; }
    internal Animacion(EfectoMultimedia efecto) : this(efecto.Jugador, 0, 0)
    {
        Efecto = efecto;
        Presentacion = ControladorMultimedia.Definir(efecto);
    }

    // Prepara los datos que usa Animacion.
    public Animacion(string id, int desde, int pasos)
    {
        Id = id;
        Desde = desde;
        Pasos = pasos;
    }
}

public sealed class MotorAnimacion
{
    private readonly object bloqueo = new();
    private readonly ColaCircular<Animacion> cola = new(128);
    public string TextoSilencio { get; internal set; } = "Silenciar";
    public string TextoFondo { get; internal set; } = "Fondo: no";
    private int dado1;
    private int dado2;
    private int total;
    public int TotalDados => total;

    private bool hardware;
    public event Action? Actualizado;
    public event Action<string, int>? CasillaAlcanzada;
    // Solo velocidad visual; las reglas y posiciones siguen viniendo del servidor.
    public double MilisegundosPorPaso { get; set; } = 150;
    public bool EstaMoviendo(string id)
    {
        lock (bloqueo)
            foreach (var a in cola.Instantanea())
                if (a.Efecto is null) return a.Id == id;
        return false;
    }
    public bool HayPendientes
    {
        get
        {
            lock (bloqueo)
            {
                foreach (var a in cola.Instantanea()) if (a.Efecto is null) return true;
                return false;
            }
        }
    }

    // Guarda las caras de los dados que mandó el servidor.
    public void Dados(int uno, int dos, bool esHardware, int suma = 0)
    {
        lock (bloqueo)
        {
            dado1 = uno;
            dado2 = dos;
            total = suma == 0 ? uno + dos : suma;
            hardware = esHardware;
        }
    }

    // Prepara la animación entre las posiciones que mandó el servidor.
    public void Mover(string id, int desde, int hasta, bool pasoInicio, int cantidad)
    {
        int pasos = hasta - desde;
        if (pasoInicio)
        {
            pasos += cantidad;
        }

        Recorrido(id, desde, hasta, pasos, false);
    }

    // Guarda el recorrido del servidor, incluyendo retrocesos y saltos.
    public void Recorrido(string id, int desde, int hasta, int pasos, bool salto)
    {
        lock (bloqueo)
        {
            if (salto)
            {
                if (!cola.Encolar(new Animacion(id, hasta, 0)))
                {
                    throw new InvalidOperationException("La cola de animaciones esta llena.");
                }

                return;
            }

            if (pasos != 0 && !cola.Encolar(new Animacion(id, desde, pasos)))
            {
                throw new InvalidOperationException("La cola de animaciones esta llena.");
            }
        }
    }

    // Avanza las animaciones usando el tiempo que recibe del reloj.
    public void Avanzar(double milisegundos, bool publicar = true)
    {
        Animacion? llegada = null;
        double paso = Math.Clamp(MilisegundosPorPaso, 50, 5000);
        lock (bloqueo)
        {
            int cantidad = cola.Cantidad;
            for (int n = 0; n < cantidad; n++)
            {
                cola.Desencolar(out var animacion);
                if (animacion.Efecto is not null || milisegundos <= 0) { cola.Encolar(animacion); continue; }
                double pendiente = Math.Abs(animacion.Pasos) * paso - animacion.Transcurrido;
                double avance = Math.Min(milisegundos, pendiente);
                animacion.Transcurrido += avance;
                milisegundos -= avance;
                if (animacion.Transcurrido < Math.Abs(animacion.Pasos) * paso) cola.Encolar(animacion);
                else llegada = animacion;
            }
        }
        // Solo al terminar el recorrido: las casillas intermedias no activan música.
        // Si un tick completa varios recorridos, se reproduce el último destino alcanzado.
        if (llegada is not null) CasillaAlcanzada?.Invoke(llegada.Id, llegada.Desde + llegada.Pasos);
        if (publicar) Publicar();
    }
    public void Publicar() => Actualizado?.Invoke();

    public void EncolarEfecto(EfectoMultimedia efecto)
    {
        lock (bloqueo)
            if (!cola.Encolar(new Animacion(efecto))) throw new InvalidOperationException("La cola de animaciones esta llena.");
    }
    // Las solicitudes de música comparten la cola enlazada original de recorridos.
    public EfectoMultimedia? ExtraerMusicaPendiente()
    {
        lock (bloqueo)
        {
            if (HayPendientes) return null;
            EfectoMultimedia? sonido = null; int prioridad = 0;
            while (cola.Desencolar(out var animacion))
            {
                var definicion = animacion.Presentacion;
                if (definicion is not null && definicion.Pista.Length > 0 && definicion.Prioridad >= prioridad)
                { sonido = animacion.Efecto; prioridad = definicion.Prioridad; }
            }
            return sonido;
        }
    }
    public void AgregarPresentacion(ListaSimple<ElementoEscena> elementos)
    {
        elementos.Agregar(new ElementoEscena("CONTROL", new Rectangulo(0, 0, 0, 0), TextoSilencio, "silenciar"));
        elementos.Agregar(new ElementoEscena("CONTROL", new Rectangulo(0, 0, 0, 0), TextoFondo, "fondo"));
    }
    public void QuitarEfectos()
    {
        lock (bloqueo)
        {
            int cantidad = cola.Cantidad;
            for (int n = 0; n < cantidad; n++)
            {
                cola.Desencolar(out var animacion);
                if (animacion.Efecto is null) cola.Encolar(animacion);
            }
        }
    }

    // Devuelve los dados recibidos, sin generar números al azar.
    public (int Uno, int Dos, bool Hardware) ObtenerDados()
    {
        lock (bloqueo)
        {
            return (dado1, dado2, hardware);
        }
    }

    // Devuelve dónde se dibuja la ficha sin cambiar su posición oficial.
    public double PosicionVisual(string id, int posicionOficial, int cantidad)
    {
        lock (bloqueo)
        {
            foreach (Animacion animacion in cola.Instantanea())
            {
                if (animacion.Efecto is null && animacion.Id == id)
                {
                    double posicion = animacion.Desde + Math.Sign(animacion.Pasos) * animacion.Transcurrido / Math.Clamp(MilisegundosPorPaso, 50, 5000);
                    return (posicion % cantidad + cantidad) % cantidad;
                }
            }
        }

        return posicionOficial;
    }

    // Borra las animaciones cuando se vuelve a cargar el estado.
    public void Reiniciar()
    {
        lock (bloqueo)
        {
            cola.Limpiar();
            dado1 = 0;
            dado2 = 0;
            total = 0;
        }
    }
}
