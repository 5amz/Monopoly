using System;

namespace Monopoly.Nucleo;

public sealed record DefinicionEfecto(string Pista, int Prioridad);



public sealed class ControladorMultimedia : IDisposable
{
    private readonly MusicaCiudad musica;
    private readonly Func<long> ahora;
    private Func<EstadoLocal?>? leerEstado;
    private MotorAnimacion? motor;
    private long ultimoTick;
    private bool detenido;
// Crea el objeto.
    public ControladorMultimedia(string carpetaMusica, Action<string> informar, IAudioCiudad salida, Func<long>? reloj = null)
    {
        ahora = reloj ?? (() => Environment.TickCount64);
        musica = new MusicaCiudad(carpetaMusica, informar, salida, ahora);
    }
// Ejecuta Definir.
    internal static DefinicionEfecto Definir(EfectoMultimedia efecto) => efecto.Tipo switch
    {
        "eliminado" => new("derrota", 20),
        "victoria" => new("victoria", 30),
        _ when efecto.Carta is not null => new(efecto.Carta.Imagen, 10),
        _ => new("", 0)
    };
// Ejecuta Vincular.
    public void Vincular(MotorAnimacion nuevo, Func<EstadoLocal?>? estado = null)
    {
        if (motor is not null) motor.CasillaAlcanzada -= AlCaerEnCasilla;
        motor = nuevo; leerEstado = estado;
        motor.MilisegundosPorPaso = musica.MilisegundosPorCasilla;
        motor.CasillaAlcanzada += AlCaerEnCasilla;
        ultimoTick = ahora();
        if (leerEstado?.Invoke() is EstadoLocal actual) musica.Preparar(actual);
        ActualizarTextos(); musica.Iniciar();
    }
// Ejecuta AlCaerEnCasilla.
    private void AlCaerEnCasilla(string jugador, int posicion)
    {
        var estado = leerEstado?.Invoke();
        if (estado is null || estado.Casillas.Cantidad == 0) return;
        int cantidad = estado.Casillas.Cantidad;
        musica.Casilla(estado.Casillas.Obtener((posicion % cantidad + cantidad) % cantidad));
    }
// Ejecuta Recibir.
    public void Recibir(EfectoMultimedia efecto)
    {
        if (!detenido) motor?.EncolarEfecto(efecto);
    }
// Ejecuta AlternarSilencio.
    public void AlternarSilencio() { musica.Silenciar(!musica.Silenciado); ActualizarTextos(); motor?.Publicar(); }
// Ejecuta AlternarFondo.
    public void AlternarFondo() { musica.ActivarFondo(!musica.FondoActivado); ActualizarTextos(); motor?.Publicar(); }
// Ejecuta ActualizarTextos.
    private void ActualizarTextos()
    {
        if (motor is null) return;
        motor.TextoSilencio = musica.Silenciado ? "Activar sonido" : "Silenciar";
        motor.TextoFondo = musica.FondoActivado ? "Fondo: sí" : "Fondo: no";
    }
// Ejecuta Actualizar.
    public void Actualizar()
    {
        if (detenido) return;
        long tiempo = ahora();
        if (leerEstado?.Invoke() is EstadoLocal estado) musica.Preparar(estado);
        motor?.Avanzar(Math.Clamp(tiempo - ultimoTick, 1, 80), publicar: false);
        ultimoTick = tiempo;
        var sonido = motor?.ExtraerMusicaPendiente();
        if (sonido?.Carta is not null) musica.Carta(sonido.Carta);
        else if (sonido is not null) musica.Evento(Definir(sonido).Pista);
        musica.Actualizar();
        motor?.Publicar();
    }
// Ejecuta Dispose.
    public void Dispose()
    {
        if (detenido) return;
        detenido = true;
        if (motor is not null) { motor.CasillaAlcanzada -= AlCaerEnCasilla; motor.QuitarEfectos(); }
        musica.Dispose();
    }
}
