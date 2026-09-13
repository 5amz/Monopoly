using System;

namespace Monopoly
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfiguradorTablero configurador = new ConfiguradorTablero();

            Tablero tablero = configurador.CrearTablero();

            Console.WriteLine("=== TABLERO ===");
            Console.WriteLine("Cantidad de casillas: " + tablero.Cantidad);
            Console.WriteLine();

            for (int i = 0; i < tablero.Cantidad; i++)
            {
                Casilla casilla = tablero.ObtenerCasilla(i);

                Console.WriteLine(
                    i + " -> " + casilla.ObtenerInformacion()
                );
            }

            Console.WriteLine();
            Console.WriteLine("=== PRUEBA DE CIRCULARIDAD ===");

            Console.WriteLine(
                "Primero: " +
                tablero.Head.Casilla.Nombre
            );

            Console.WriteLine(
                "Ultimo: " +
                tablero.Tail.Casilla.Nombre
            );

            Console.WriteLine(
                "Ultimo.Siguiente: " +
                tablero.Tail.Next.Casilla.Nombre
            );

            Console.WriteLine(
                "Primero.Anterior: " +
                tablero.Head.Prev.Casilla.Nombre
            );
        }
    }
}