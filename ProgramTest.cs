using Monopoly.Administracion;

namespace Monopoly
{
    /// <summary>Demostración local de servidor, tablero, turnos y Banco integrados.</summary>
    internal class Program
    {
        private static void Main()
        {
            var servidor = new ServidorJuego();
            Tablero tablero = new ConfiguradorTablero().CrearTablero();
            var colaTurnos = new ColaTurnos();
            var coordinador = new CoordinadorPartidaTablero(servidor, tablero, colaTurnos);
            servidor.ConfigurarModulos(coordinador, coordinador, coordinador, coordinador);

            servidor.RegistrarJugador("J1", "Ana", 500000m);
            servidor.RegistrarJugador("J2", "Luis", 500000m);
            servidor.MarcarPartidaIniciada();

            Console.WriteLine("=== COMPRA CON TURNO Y BANCO OFICIAL ===");
            Console.WriteLine(coordinador.MoverJugador("J1", 1).Mensaje);
            Console.WriteLine(coordinador.ComprarPropiedad("J1").Mensaje);
            Console.WriteLine($"Saldo oficial de Ana: {servidor.Banco.ConsultarSaldo("J1")}");

            Console.WriteLine("\n=== ALQUILER CON TURNO Y BANCO OFICIAL ===");
            Console.WriteLine(coordinador.TerminarTurno("J1").Mensaje);
            Console.WriteLine(coordinador.MoverJugador("J2", 1).Mensaje);
            Console.WriteLine(coordinador.PagarAlquilerActual("J2").Mensaje);
            Console.WriteLine($"Saldo oficial de Ana: {servidor.Banco.ConsultarSaldo("J1")}");
            Console.WriteLine($"Saldo oficial de Luis: {servidor.Banco.ConsultarSaldo("J2")}");

            Console.WriteLine("\n=== PROPIEDAD DE LUIS ===");
            Console.WriteLine(coordinador.MoverJugador("J2", 2).Mensaje);
            Console.WriteLine(coordinador.ComprarPropiedad("J2").Mensaje);
            Propiedad propiedadDeLuis = (Propiedad)tablero.ObtenerCasilla(3);

            Console.WriteLine("\n=== ELIMINACIÓN POR INSOLVENCIA ===");
            ResultadoOperacion pagoImposible = servidor.ProcesarPagoAlquiler(
                "J2",
                "J1",
                999999m,
                "Avenida Central",
                numeroTurno: 2);
            Console.WriteLine(pagoImposible.Mensaje);
            Console.WriteLine($"Luis activo: {servidor.Banco.ConsultarJugador("J2").EstaActivo}");
            Console.WriteLine($"Jugadores en cola: {colaTurnos.Cantidad}");
            Console.WriteLine($"Propiedad de Luis liberada: {propiedadDeLuis.Disponible}");
            Console.WriteLine($"Estado de partida: {servidor.Estado}");

            Console.WriteLine("\n=== TRANSACCIONES ===");
            Console.WriteLine(servidor.Banco.Historial.GenerarReporteCompleto());
        }
    }
}
