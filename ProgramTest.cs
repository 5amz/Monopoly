using Monopoly.Administracion;

namespace Monopoly
{
    /// <summary>
    /// Demostración local de la separación entre tablero y administración.
    /// No es un cliente TCP ni una interfaz de juego.
    /// </summary>
    internal class Program
    {
        private static void Main()
        {
            var servidor = new ServidorJuego();
            servidor.RegistrarJugador("J1", "Ana", 500000m);
            servidor.RegistrarJugador("J2", "Luis", 500000m);
            servidor.MarcarPartidaIniciada();

            Tablero tablero = new ConfiguradorTablero().CrearTablero();
            var anaTablero = new JugadorTablero("J1", "Ana", tablero.Head);
            var luisTablero = new JugadorTablero("J2", "Luis", tablero.Head);

            Console.WriteLine("=== MOVIMIENTO SIN CAMBIAR SALDO LOCAL ===");
            ResultadoMovimientoTablero movimiento = tablero.MoverJugador(anaTablero.Posicion, 2, anaTablero);
            Console.WriteLine($"Casilla final: {movimiento.PosicionFinal.Casilla.Nombre}");
            Console.WriteLine($"Pasos por inicio: {movimiento.VecesPasoPorInicio}");

            Propiedad propiedad = (Propiedad)tablero.ObtenerCasilla(1);
            Console.WriteLine("\n=== COMPRA AUTORIZADA POR BANCO ===");
            if (tablero.PuedeComprarPropiedad(anaTablero, propiedad))
            {
                ResultadoOperacion compra = servidor.ProcesarCompraPropiedad(
                    anaTablero.IdJugador,
                    propiedad.Precio,
                    propiedad.Nombre,
                    numeroTurno: 1);

                if (compra.FueExitosa)
                    tablero.AsignarPropiedad(anaTablero, propiedad);

                Console.WriteLine(compra.Mensaje);
            }

            Console.WriteLine($"Propietario en tablero: {propiedad.Propietario.Nombre}");
            Console.WriteLine($"Saldo oficial de Ana: {servidor.Banco.ConsultarSaldo("J1")}");

            Console.WriteLine("\n=== ALQUILER AUTORIZADO POR BANCO ===");
            if (tablero.PuedePagarAlquiler(luisTablero, propiedad))
            {
                ResultadoOperacion alquiler = servidor.ProcesarPagoAlquiler(
                    luisTablero.IdJugador,
                    propiedad.Propietario.IdJugador,
                    propiedad.Alquiler,
                    propiedad.Nombre,
                    numeroTurno: 2);

                Console.WriteLine(alquiler.Mensaje);
            }

            Console.WriteLine($"Saldo oficial de Ana: {servidor.Banco.ConsultarSaldo("J1")}");
            Console.WriteLine($"Saldo oficial de Luis: {servidor.Banco.ConsultarSaldo("J2")}");
            Console.WriteLine("\n=== TRANSACCIONES DE ANA ===");
            Console.WriteLine(servidor.Banco.Historial.GenerarReportePorJugador("J1"));
        }
    }
}
