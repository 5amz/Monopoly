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

            MazoEventos mazo = new MazoEventos();

            mazo.AgregarCarta(new CartaEvento(1, "Recibe dinero", "GanarDinero", 50000));

            mazo.AgregarCarta(new CartaEvento(2, "Paga una multa", "PerderDinero", 30000));

            mazo.AgregarCarta(new CartaEvento(3, "Avanza 3 casillas", "Avanzar", 3));

            Console.WriteLine();
            Console.WriteLine("=== MAZO DE EVENTOS ===");
            Console.WriteLine("Cantidad de cartas: " + mazo.Cantidad);

            CartaEvento carta1 = mazo.ObtenerSiguienteCarta();
            Console.WriteLine("Carta obtenida: " + carta1.Descripcion);

            CartaEvento carta2 = mazo.ObtenerSiguienteCarta();
            Console.WriteLine("Carta obtenida: " + carta2.Descripcion);

            CartaEvento carta3 = mazo.ObtenerSiguienteCarta();
            Console.WriteLine("Carta obtenida: " + carta3.Descripcion);

            CartaEvento carta4 = mazo.ObtenerSiguienteCarta();
            Console.WriteLine("Carta obtenida nuevamente: " + carta4.Descripcion);

            Console.WriteLine();
            Console.WriteLine("=== COLA DE TURNOS ===");

            ColaTurnos colaTurnos = new ColaTurnos();

            colaTurnos.AgregarJugador(1, "Ana");
            colaTurnos.AgregarJugador(2, "Carlos");
            colaTurnos.AgregarJugador(3, "Maria");
            colaTurnos.AgregarJugador(4, "Pedro");

            Console.WriteLine("Cantidad de jugadores: " + colaTurnos.Cantidad);

            Console.WriteLine("Jugador actual: " +colaTurnos.ObtenerJugadorActual().Nombre);

            colaTurnos.AvanzarTurno();

            Console.WriteLine("Despues de avanzar: " +colaTurnos.ObtenerJugadorActual().Nombre);

            colaTurnos.AvanzarTurno();

            Console.WriteLine("Despues de avanzar: " +colaTurnos.ObtenerJugadorActual().Nombre);

            colaTurnos.AvanzarTurno();

            Console.WriteLine("Despues de avanzar: " +colaTurnos.ObtenerJugadorActual().Nombre);

            colaTurnos.AvanzarTurno();

            Console.WriteLine("Despues de volver al inicio: " +colaTurnos.ObtenerJugadorActual().Nombre);
        }
    }
}