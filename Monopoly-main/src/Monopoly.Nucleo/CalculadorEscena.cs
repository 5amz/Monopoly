using System;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;
public sealed class CalculadorEscena
{
    private readonly GeometriaTablero geometria = new();
    // Prepara las imágenes, los textos y sus posiciones para dibujar el tablero.
    public EscenaTablero Calcular(EstadoLocal estado, MotorAnimacion motor, double ancho, double alto, CartaVisible? carta = null)
    {
        ListaSimple<ElementoEscena> elementos = new();
        ListaSimple<Rectangulo> lugares = geometria.Calcular(ancho, alto);
        var esquina = lugares.Obtener(12);
        double x = esquina.X, y = esquina.Y, w = esquina.Ancho, h = esquina.Alto;
        // El fondo pertenece a la ventana: dibujarlo aquí lo oculta bajo centro y casillas.
        elementos.Agregar(new ElementoEscena("IMAGEN", new Rectangulo(x + w, y + h, w * 5, h * 5), "logo_central"));
        int posicion = 0;
        foreach (CasillaVista casilla in estado.Casillas)
        {
            Rectangulo lugar = lugares.Obtener(posicion);
            elementos.Agregar(new ElementoEscena("IMAGEN", lugar, casilla.Imagen));
            if (casilla.Tipo == "PROPIEDAD" && casilla.IdPropietario is "J1" or "J2" or "J3" or "J4")
                elementos.Agregar(new ElementoEscena("IMAGEN", lugar, "propiedad_" + casilla.IdPropietario.ToLowerInvariant()));
            string insignia = casilla.Tipo == "PROPIEDAD" ? "P" : casilla.Tipo == "EVENTO" ? "?" : casilla.Id == 1 ? ">" : casilla.Id is 5 or 17 ? "−" : "=";
            elementos.Agregar(new ElementoEscena("TIPO", new Rectangulo(lugar.X + lugar.Ancho * .76, lugar.Y + 3, lugar.Ancho * .21, lugar.Alto * .24), insignia));
            string importe = casilla.Tipo == "PROPIEDAD"
                ? "Compra ₡" + casilla.Precio.ToString("N0") + "\nAlq. ₡" + casilla.Alquiler.ToString("N0")
                : casilla.Id is 1 or 5 or 17 ? (casilla.Id == 1 ? "+₡" : "−₡") + casilla.Precio.ToString("N0") : "";
            if (importe.Length > 0)
                elementos.Agregar(new ElementoEscena("PRECIO", new Rectangulo(lugar.X + 2, lugar.Y + lugar.Alto * .66, lugar.Ancho - 4, lugar.Alto * .33), importe));
            string detalle = casilla.Id + ". " + casilla.Cancion + "\n" + casilla.Artista;
            if (casilla.Tipo == "PROPIEDAD")
            {
                detalle += "\nPrecio: ₡" + casilla.Precio.ToString("N0") + " | Alquiler: ₡" + casilla.Alquiler.ToString("N0") + "\nDueño: " + casilla.IdPropietario;
            }

            elementos.Agregar(new ElementoEscena("PISTA", lugar, detalle));
            posicion++;
        }

        // La parte superior de la pila siempre ocupa el mismo lugar; la carta se superpone.
        var pila = new Rectangulo(x + w * 1.15, y + h * 5.05, w * 2, h * 0.8);
        elementos.Agregar(new ElementoEscena("IMAGEN", pila, "mazo_eventos"));
        if (carta is not null)
        {
            double altoCarta = pila.Ancho * 1.4;
            var frente = new Rectangulo(pila.X, pila.Y + pila.Alto - altoCarta - h * 0.1, pila.Ancho, altoCarta);
            elementos.Agregar(new ElementoEscena("IMAGEN", frente, carta.Imagen));
            elementos.Agregar(new ElementoEscena("PISTA", frente,
                carta.Cancion + "\n" + carta.Texto + "\n" + carta.Efecto + ": " + carta.Valor));
        }

        var dados = motor.ObtenerDados();
        double ladoDado = Math.Min(w * .95, h * 1.2);
        double dx = x + 6 * w - ladoDado * 2 - w * .2, dy = y + 6 * h - ladoDado - h * .5;
        if (dados.Uno > 0) elementos.Agregar(new ElementoEscena("IMAGEN", new Rectangulo(dx, dy, ladoDado, ladoDado), "dado_" + dados.Uno));
        if (dados.Dos > 0) elementos.Agregar(new ElementoEscena("IMAGEN", new Rectangulo(dx + ladoDado + w * .06, dy, ladoDado, ladoDado), "dado_" + dados.Dos));
        elementos.Agregar(new ElementoEscena("TEXTO", new Rectangulo(dx, dy + ladoDado + 3, ladoDado * 2.06, h * .4), motor.TotalDados == 0 ? "Esperando dados" : "Total: " + motor.TotalDados + (dados.Hardware ? " · Pico" : " · Prueba")));

        foreach (JugadorVista jugador in estado.Jugadores)
        {
            if (jugador.Activo)
            {
                Rectangulo lugarFicha = UbicarFicha(jugador, estado, lugares, motor);
                elementos.Agregar(new ElementoEscena("IMAGEN", lugarFicha, "ficha_" + jugador.Id.ToLowerInvariant()));
            }

        }

        motor.AgregarPresentacion(elementos);
        return new EscenaTablero(elementos.Congelar());
    }

    // Calcula el lugar de la ficha durante la animación y evita que tape otras fichas.
    private Rectangulo UbicarFicha(JugadorVista jugador, EstadoLocal estado, ListaSimple<Rectangulo> lugares, MotorAnimacion motor)
    {
        double posicion = motor.PosicionVisual(jugador.Id, jugador.Posicion, lugares.Cantidad);
        int desde = (int)Math.Floor(posicion);
        int hasta = (desde + 1) % lugares.Cantidad;
        double avance = posicion - desde;
        Rectangulo inicio = lugares.Obtener(desde);
        Rectangulo final = lugares.Obtener(hasta);
        double x = inicio.X + (final.X - inicio.X) * avance;
        double y = inicio.Y + (final.Y - inicio.Y) * avance;
        int juntos = 0, numero = 0;
        foreach (var otro in estado.Jugadores)
        {
            if (!otro.Activo || Math.Abs(motor.PosicionVisual(otro.Id, otro.Posicion, lugares.Cantidad) - posicion) > .05) continue;
            if (string.CompareOrdinal(otro.Id, jugador.Id) < 0) numero++;
            juntos++;
        }
        bool grande = motor.EstaMoviendo(jugador.Id) || juntos <= 1;
        double tamano = Math.Min(inicio.Ancho, inicio.Alto) * (grande ? .48 : .27);
        x += grande ? (inicio.Ancho - tamano) / 2 : inicio.Ancho * (.18 + numero % 2 * .38);
        y += grande ? inicio.Alto * .15 : inicio.Alto * (.08 + numero / 2 * .29);
        return new Rectangulo(x, y, tamano, tamano);
    }
}
