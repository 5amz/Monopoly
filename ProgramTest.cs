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
                "Propietario paga su propio alquiler: " + alquilerPropietario);

            Console.WriteLine();
            Console.WriteLine("=== EVENTO: GANAR DINERO ===");

            JugadorTablero jugadorEvento = new JugadorTablero(6, "Juan", tablero.Head);

            jugadorEvento.Dinero = 100000;

            CartaEvento cartaGanar = new CartaEvento(1, "Recibe dinero", "GanarDinero", 50000);

            Console.WriteLine("Dinero antes: " + jugadorEvento.Dinero);

            tablero.EjecutarCartaEvento(cartaGanar, jugadorEvento);

            Console.WriteLine("Dinero despues: " + jugadorEvento.Dinero);


            Console.WriteLine();
            Console.WriteLine("=== EVENTO: PERDER DINERO ===");

            CartaEvento cartaPerder = new CartaEvento(2, "Paga una multa", "PerderDinero", 30000);

            Console.WriteLine("Dinero antes: " + jugadorEvento.Dinero);

            tablero.EjecutarCartaEvento(cartaPerder, jugadorEvento);

            Console.WriteLine("Dinero despues: " + jugadorEvento.Dinero);


            Console.WriteLine();
            Console.WriteLine("=== EVENTO: AVANZAR ===");

            jugadorEvento.Posicion = tablero.ObtenerNodo(5);

            CartaEvento cartaAvanzar = new CartaEvento(3, "Avanza 3 casillas", "Avanzar", 3);

            Console.WriteLine("Posicion antes: " + jugadorEvento.Posicion.Casilla.Nombre);

            tablero.EjecutarCartaEvento(cartaAvanzar, jugadorEvento);

            Console.WriteLine("Posicion despues: " + jugadorEvento.Posicion.Casilla.Nombre);


            Console.WriteLine();
            Console.WriteLine("=== EVENTO: RETROCEDER ===");

            CartaEvento cartaRetroceder = new CartaEvento(4, "Retrocede 2 casillas", "Retroceder",2);

            Console.WriteLine("Posicion antes: " + jugadorEvento.Posicion.Casilla.Nombre);

            tablero.EjecutarCartaEvento(cartaRetroceder, jugadorEvento);

            Console.WriteLine("Posicion despues: " + jugadorEvento.Posicion.Casilla.Nombre);


            Console.WriteLine();
            Console.WriteLine("=== EVENTO: PERDER TURNO ===");

            CartaEvento cartaPerderTurno = new CartaEvento(5, "Pierde el siguiente turno", "PerderTurno", 0);

            Console.WriteLine("Pierde turno antes: " + jugadorEvento.PierdeTurno);

            tablero.EjecutarCartaEvento(cartaPerderTurno, jugadorEvento);

            Console.WriteLine("Pierde turno despues: " + jugadorEvento.PierdeTurno);


            Console.WriteLine();
            Console.WriteLine("=== EVENTO: IR A CASILLA ===");

            CartaEvento cartaIr = new CartaEvento(6, "Ve a una casilla especifica", "IrACasilla", 10);

            tablero.EjecutarCartaEvento(cartaIr, jugadorEvento);

            Console.WriteLine("Posicion despues de la carta: " + jugadorEvento.Posicion.Casilla.Nombre);

            Console.WriteLine("Posicion numerica: " + 10);


            Console.WriteLine();
            Console.WriteLine("=== PERDIDA DE TURNO ===");

            ColaTurnos colaPrueba = new ColaTurnos();

            JugadorTablero ana = new JugadorTablero(1, "Ana", tablero.Head);

            JugadorTablero carlos = new JugadorTablero(2, "Carlos", tablero.Head);

            JugadorTablero pedro = new JugadorTablero(3, "Pedro", tablero.Head);

            colaPrueba.AgregarJugador(ana);
            colaPrueba.AgregarJugador(carlos);
            colaPrueba.AgregarJugador(pedro);

            Console.WriteLine("Jugador actual: " + colaPrueba.ObtenerJugadorActual().Nombre);

            colaPrueba.AvanzarTurno(ana);

            Console.WriteLine("Siguiente jugador: " +colaPrueba.ObtenerJugadorActual().Nombre);

            carlos.PierdeTurno = true;

            Console.WriteLine();
            Console.WriteLine("Carlos pierde turno: " + carlos.PierdeTurno);

            colaPrueba.AvanzarTurno(carlos);

            Console.WriteLine("Jugador despues de Carlos: " + colaPrueba.ObtenerJugadorActual().Nombre);

            Console.WriteLine("Carlos mantiene perdida pendiente: " + carlos.PierdeTurno);

            colaPrueba.AvanzarTurno(pedro);

            Console.WriteLine("Jugador despues de Pedro: " + colaPrueba.ObtenerJugadorActual().Nombre);


            Console.WriteLine();
            Console.WriteLine("=== ELIMINACION DE JUGADOR ===");

            JugadorTablero jugadorEliminado = new JugadorTablero(7, "Roberto", tablero.Head);

            Propiedad propiedadRoberto = (Propiedad)tablero.ObtenerCasilla(3);

            jugadorEliminado.Dinero = 300000;

            bool comproRoberto = tablero.ComprarPropiedad(jugadorEliminado,propiedadRoberto);

            Console.WriteLine("Compra realizada: " + comproRoberto);

            Console.WriteLine("Activo antes: " + jugadorEliminado.Activo);

            Console.WriteLine("Propietario antes: " + propiedadRoberto.Propietario.Nombre);

            tablero.EliminarJugador(jugadorEliminado);

            Console.WriteLine("Activo despues: " + jugadorEliminado.Activo);

            Console.WriteLine("Propietario despues: " + (propiedadRoberto.Propietario == null));

            Console.WriteLine("Propiedad disponible: " + propiedadRoberto.Disponible);

            JugadorTablero jugadorA = new JugadorTablero(8, "Mario", tablero.Head);

            JugadorTablero jugadorB = new JugadorTablero(9, "Laura", tablero.Head);

            jugadorA.Dinero = 300000;
            jugadorB.Dinero = 400000;

            Propiedad propiedadMario = (Propiedad)tablero.ObtenerCasilla(5);

            tablero.ComprarPropiedad(jugadorA, propiedadMario);


            Console.WriteLine();
            Console.WriteLine("=== PATRIMONIO ===");

            Console.WriteLine("Patrimonio de Mario: " + tablero.CalcularPatrimonio(jugadorA));

            Console.WriteLine("Patrimonio de Laura: " + tablero.CalcularPatrimonio(jugadorB));

            JugadorTablero[] jugadores = new JugadorTablero[]
            {
                jugadorA,
                jugadorB,
                jugadorEliminado
            };

            JugadorTablero ganador = tablero.ObtenerGanador(jugadores);

            Console.WriteLine();
            Console.WriteLine("=== GANADOR ===");

            Console.WriteLine("Ganador: " + ganador.Nombre);

            Console.WriteLine("Patrimonio: " + tablero.CalcularPatrimonio(ganador));

            bool termino = tablero.PartidaTerminada(jugadores, 10, 20);

            Console.WriteLine("Partida terminada: " + termino);

            termino = tablero.PartidaTerminada(jugadores, 20, 20);
            Console.WriteLine("Partida terminada en turno 20: " + termino);
        }
    }
}