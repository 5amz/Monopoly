using Monopoly.Administracion;
using Monopoly.Hardware;

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
            using var dados = new LectorHardwareSerial("COM1");
            var coordinador = new CoordinadorPartidaTablero(
                servidor,
                tablero,
                colaTurnos,
                proveedorDados: dados);
            servidor.ConfigurarModulos(coordinador, coordinador, coordinador, coordinador);

            servidor.RegistrarJugador("J1", "Ana");
            servidor.RegistrarJugador("J2", "Luis");
            servidor.MarcarPartidaIniciada();

            Console.WriteLine("=== DADOS Y COMPRA CON BANCO OFICIAL ===");
            dados.ProcesarLineaRecibida("DADO|3");
            Console.WriteLine(servidor.ProcesarSolicitud(
                "J1",
                SolicitudProtocolo.CrearAccion(ComandoProtocolo.TIRAR_DADOS)).ConvertirALinea());
            Console.WriteLine(servidor.ProcesarSolicitud(
                "J1",
                SolicitudProtocolo.CrearAccion(ComandoProtocolo.COMPRAR_PROPIEDAD)).ConvertirALinea());
            Console.WriteLine($"Saldo oficial de Ana: {servidor.Banco.ConsultarSaldo("J1")}");

            Console.WriteLine("\n=== ALQUILER AUTOMÁTICO AL TIRAR ===");
            Console.WriteLine(servidor.ProcesarSolicitud(
                "J1",
                SolicitudProtocolo.CrearAccion(ComandoProtocolo.TERMINAR_TURNO)).ConvertirALinea());
            dados.ProcesarLineaRecibida("DADO|3");
            Console.WriteLine(servidor.ProcesarSolicitud(
                "J2",
                SolicitudProtocolo.CrearAccion(ComandoProtocolo.TIRAR_DADOS)).ConvertirALinea());
            Console.WriteLine($"Saldo oficial de Ana: {servidor.Banco.ConsultarSaldo("J1")}");
            Console.WriteLine($"Saldo oficial de Luis: {servidor.Banco.ConsultarSaldo("J2")}");

            Console.WriteLine("\n=== TRANSACCIONES ===");
            Console.WriteLine(servidor.Banco.Historial.GenerarReporteCompleto());
        }
    }
}
