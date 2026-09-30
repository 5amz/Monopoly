using System;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;
public sealed class GeometriaTablero
{
    // Ubica las 24 casillas alrededor de una cuadrícula de 7 por 7.
    public ListaSimple<Rectangulo> Calcular(double ancho, double alto)
    {
        ListaSimple<Rectangulo> lugares = new();
        double celda = Math.Max(1, Math.Min(ancho - 16, (alto - 16) * 1.25)) / 7;
        double altoCelda = celda / 1.25;
        double inicioX = (ancho - celda * 7) / 2;
        double inicioY = (alto - altoCelda * 7) / 2;
        for (int posicion = 0; posicion < 24; posicion++)
        {
            int columna;
            int fila;
            if (posicion < 6)
            {
                columna = 6 - posicion;
                fila = 6;
            }
            else if (posicion < 12)
            {
                columna = 0;
                fila = 12 - posicion;
            }
            else if (posicion < 18)
            {
                columna = posicion - 12;
                fila = 0;
            }
            else
            {
                columna = 6;
                fila = posicion - 18;
            }

            Rectangulo lugar = new(inicioX + columna * celda, inicioY + fila * altoCelda, celda, altoCelda);
            lugares.Agregar(lugar);
        }

        return lugares.Congelar();
    }
}
