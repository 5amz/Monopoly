namespace Monopoly
{
    public class ConfiguradorTablero
    {
// Ejecuta CrearTablero.
        public Tablero CrearTablero(decimal premioInicio, decimal impuesto, decimal impuestoEspecial)
        {
        Tablero tablero = new Tablero { PremioInicio = premioInicio };

        tablero.AgregarCasilla(new CasillaEspecial(1, "Inicio", "Salida")); 

        tablero.AgregarCasilla(new Propiedad(2, "Avenida Central", 200000, 25000)); 

        tablero.AgregarCasilla(new CasillaEvento(3, "Evento 1", "Recibe un beneficio")); 

        tablero.AgregarCasilla(new Propiedad(4, "Avenida Norte", 220000, 28000)); 

        tablero.AgregarCasilla(new CasillaEspecial(5, "Impuesto", "Pago", impuesto)); 

        tablero.AgregarCasilla(new Propiedad(6, "Calle del Sol", 250000, 30000)); 

        tablero.AgregarCasilla(new CasillaEvento(7, "Evento 2", "Pierde dinero")); 

        tablero.AgregarCasilla(new Propiedad(8, "Avenida del Parque", 280000, 35000)); 

        tablero.AgregarCasilla(new CasillaEspecial(9, "Descanso", "Especial")); 

        tablero.AgregarCasilla(new Propiedad(10, "Calle Central", 300000, 38000)); 

        tablero.AgregarCasilla(new CasillaEvento(11, "Evento 3", "Avanza algunas casillas")); 

        tablero.AgregarCasilla(new Propiedad(12, "Avenida Este", 320000, 40000)); 

        tablero.AgregarCasilla(new CasillaEspecial(13, "Visita", "Especial")); 

        tablero.AgregarCasilla(new Propiedad(14, "Calle Verde", 350000, 45000)); 

        tablero.AgregarCasilla(new CasillaEvento(15, "Evento 4", "Retrocede algunas casillas")); 

        tablero.AgregarCasilla(new Propiedad(16, "Avenida del Lago", 380000, 48000)); 

        tablero.AgregarCasilla(new CasillaEspecial(17, "Impuesto Especial", "Pago", impuestoEspecial)); 

        tablero.AgregarCasilla(new Propiedad(18, "Calle Principal", 400000, 50000)); 

        tablero.AgregarCasilla(new CasillaEvento(19, "Evento 5", "Pierde un Turno")); 

        tablero.AgregarCasilla(new Propiedad(20, "Avenida Sur", 420000, 55000)); 

        tablero.AgregarCasilla(new CasillaEspecial(21, "Estacionamiento", "Especial")); 

        tablero.AgregarCasilla(new Propiedad(22, "Calle del Bosque", 450000, 58000)); 

        tablero.AgregarCasilla(new CasillaEvento(23, "Evento 6", "Recibe dinero")); 

        tablero.AgregarCasilla(new Propiedad(24, "Avenida del Mar", 500000, 65000)); 

        return tablero;

        }
    }
}
