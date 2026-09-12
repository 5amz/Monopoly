using System;

namespace Monopoly
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== PRUEBA DE JERARQUIA DE CASILLAS ===");
            Console.WriteLine();

            // 1. Casilla base
            Casilla casilla = new Casilla(
                1,
                "Casilla Base"
            );

            // 2. Propiedad
            Casilla propiedad = new Propiedad(
                2,
                "Avenida Central",
                200000,
                25000
            );

            // 3. Casilla de evento
            Casilla evento = new CasillaEvento(
                3,
                "Suerte",
                "Recibe un premio"
            );

            // 4. Casilla especial
            Casilla especial = new CasillaEspecial(
                4,
                "Inicio",
                "Salida"
            );

            // Mostrar información
            Console.WriteLine("Casilla:");
            Console.WriteLine(casilla.ObtenerInformacion());
            Console.WriteLine();

            Console.WriteLine("Propiedad:");
            Console.WriteLine(propiedad.ObtenerInformacion());
            Console.WriteLine();

            Console.WriteLine("Casilla de evento:");
            Console.WriteLine(evento.ObtenerInformacion());
            Console.WriteLine();

            Console.WriteLine("Casilla especial:");
            Console.WriteLine(especial.ObtenerInformacion());
            Console.WriteLine();

            // Comprobar herencia
            Console.WriteLine("=== PRUEBA DE HERENCIA ===");

            Console.WriteLine(
                "Propiedad es Casilla: " +
                (propiedad is Casilla)
            );

            Console.WriteLine(
                "Evento es Casilla: " +
                (evento is Casilla)
            );

            Console.WriteLine(
                "Especial es Casilla: " +
                (especial is Casilla)
            );

            Console.WriteLine();

            // Comprobar polimorfismo
            Console.WriteLine("=== PRUEBA DE POLIMORFISMO ===");

            Casilla[] casillas =
            {
                casilla,
                propiedad,
                evento,
                especial
            };

            foreach (Casilla actual in casillas)
            {
                Console.WriteLine(actual.ObtenerInformacion());
            }
        }
    }
}