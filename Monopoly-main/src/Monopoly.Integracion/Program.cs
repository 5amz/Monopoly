using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Monopoly.Integracion;

internal static class Program
{
// Ejecuta Main.
    private static int Main(string[] args)
    {
        Console.InputEncoding = new UTF8Encoding(false);
        Console.OutputEncoding = new UTF8Encoding(false);
        try
        {
            int puerto = 5000;
            var opciones = new ConfiguracionPartida();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--config")
                {
                    if (++i >= args.Length) throw new ArgumentException("Falta el archivo JSON.");
                    opciones = JsonSerializer.Deserialize<ConfiguracionPartida>(File.ReadAllText(args[i]))
                        ?? throw new ArgumentException("Configuración vacía.");
                }
            }
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--config": i++; break;
                    case "--simulado": opciones.UsarHardware = false; break;
                    case "--hardware":
                        opciones.UsarHardware = true;
                        opciones.PuertoSerial = Valor(args, ref i);
                        break;
                    case "--puerto": puerto = int.Parse(Valor(args, ref i), CultureInfo.InvariantCulture); break;
                    case "--max-turnos": opciones.MaxTurnos = int.Parse(Valor(args, ref i), CultureInfo.InvariantCulture); break;
                    case "--txt": opciones.RutaTransacciones = Valor(args, ref i); break;
                    default:
                        if (i == 0 && int.TryParse(args[i], out int valor)) puerto = valor;
                        else throw new ArgumentException("Opción desconocida: " + args[i]);
                        break;
                }
            }
            if (puerto < 1 || puerto > 65535) throw new ArgumentException("Puerto TCP fuera de rango.");
            opciones.Validar();
            using var servidor = new ServidorIntegrado(opciones);
            bool listo = false;
            servidor.ServidorListo += () => listo = true;
            servidor.ServidorFallo += Console.Error.WriteLine;
            servidor.Iniciar(puerto);
            if (!listo) return 1;
            Console.WriteLine("Servidor oficial; TCP " + servidor.PuertoEscucha);
            Console.WriteLine(opciones.UsarHardware ? "HARDWARE: Pico en " + opciones.PuertoSerial :
                "SIMULADOR DE DESARROLLO: no sustituye la demostración física. Dados simulados; pagos requieren entrada RFID.");
            Console.WriteLine("Cuatro clientes. Órdenes locales: exportar [ruta], abandonar J1, salir.");
            if (!opciones.UsarHardware)
                Console.WriteLine("Entrada de prueba: serial RFID|62384551 (J1), A98F8656 (J2), D03B9032 (J3), 2203D734 (J4). También serial DADO|1|2|3.");
            while (true)
            {
                string linea = Console.ReadLine();
                if (linea == null || linea.Equals("salir", StringComparison.OrdinalIgnoreCase)) break;
                if (linea == "exportar" || linea.StartsWith("exportar ", StringComparison.Ordinal))
                    Console.WriteLine("TXT: " + servidor.ExportarTransacciones(linea.Length > 9 ? linea[9..].Trim() : null));
                else if (linea.StartsWith("abandonar ", StringComparison.Ordinal))
                    Console.WriteLine(servidor.AbandonarJugador(linea[10..].Trim()) ? "Jugador retirado." : "Abandono rechazado.");
                else if (!opciones.UsarHardware && linea.StartsWith("serial ", StringComparison.Ordinal))
                    Console.WriteLine(servidor.RecibirLineaSerial(linea[7..]) ? "Entrada procesada." : "Entrada rechazada o sin operación aplicable.");
                else if (linea.Length > 0) Console.WriteLine("Orden local desconocida.");
            }
            return 0;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or FormatException or JsonException or OverflowException or InvalidOperationException)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine("Uso: Monopoly.Integracion [--puerto 5000] [--hardware COM3 | --simulado] [--config configuracion.json] [--max-turnos 60] [--txt reporte.txt]");
            return 1;
        }
    }

// Ejecuta Valor.
    private static string Valor(string[] args, ref int indice)
    {
        if (++indice >= args.Length) throw new ArgumentException("Falta el valor de una opción.");
        return args[indice];
    }
}
