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

            Console.WriteLine();
            Console.WriteLine("=== MOVIMIENTO DEL JUGADOR ===");

            NodoCasilla posicionInicial = tablero.ObtenerNodo(22);

            JugadorTablero jugador = new JugadorTablero(1,"Ana",posicionInicial);

            Console.WriteLine("Posicion inicial: " +jugador.Posicion.Casilla.Nombre);

            jugador.Posicion = tablero.MoverJugador(jugador.Posicion,4, jugador);

            Console.WriteLine("Posicion despues de avanzar 4 casillas: " +jugador.Posicion.Casilla.Nombre);

            Console.WriteLine("ID de la casilla final: " +jugador.Posicion.Casilla.Id);

            Console.WriteLine();
            Console.WriteLine("=== PREMIO POR PASAR POR INICIO ===");

            JugadorTablero jugador3 = new JugadorTablero(3,"Pedro",tablero.ObtenerNodo(23));

            Console.WriteLine("Dinero inicial: " +jugador3.Dinero);

            jugador3.Posicion = tablero.MoverJugador(jugador3.Posicion,1,jugador3);

            Console.WriteLine("Posicion despues de avanzar: " + jugador3.Posicion.Casilla.Nombre);

            Console.WriteLine("Dinero despues de pasar por Inicio: " + jugador3.Dinero);

            //Compra de propiedad
            Console.WriteLine();
            Console.WriteLine("=== COMPRA DE PROPIEDAD ===");

            JugadorTablero jugadorCompra = new JugadorTablero(4,"Luis",tablero.Head);

            jugadorCompra.Dinero = 500000;

            Propiedad propiedadCompra = (Propiedad)tablero.ObtenerCasilla(1);

            Console.WriteLine("Dinero antes de comprar: " + jugadorCompra.Dinero);

            Console.WriteLine("Propiedad: " + propiedadCompra.Nombre);

            Console.WriteLine("Precio: " + propiedadCompra.Precio);

            bool compraRealizada = tablero.ComprarPropiedad(jugadorCompra, propiedadCompra);

            Console.WriteLine("Compra realizada: " + compraRealizada);

            Console.WriteLine("Dinero despues de comprar: " + jugadorCompra.Dinero);

            Console.WriteLine("Propietario: " + propiedadCompra.Propietario.Nombre);

            Console.WriteLine("Disponible: " + propiedadCompra.Disponible);

            //Alquiler
            Console.WriteLine();
            Console.WriteLine("=== PAGO DE ALQUILER ===");

            JugadorTablero jugadorAlquiler = new JugadorTablero(5,"Carlos",tablero.Head);

            jugadorAlquiler.Dinero = 400000;

            Console.WriteLine("Dinero de Carlos antes: " + jugadorAlquiler.Dinero);

            Console.WriteLine("Dinero de Luis antes: " + jugadorCompra.Dinero);

            bool alquilerPagado = tablero.PagarAlquiler(jugadorAlquiler, propiedadCompra);

            Console.WriteLine("Alquiler pagado: " + alquilerPagado);

            Console.WriteLine("Dinero de Carlos despues: " + jugadorAlquiler.Dinero);

            Console.WriteLine("Dinero de Luis despues: " + jugadorCompra.Dinero);

            bool alquilerPropietario = tablero.PagarAlquiler(jugadorCompra,propiedadCompra);

            Console.WriteLine(
                "Propietario paga su propio alquiler: " +
                alquilerPropietario
            );
        }
    }
}