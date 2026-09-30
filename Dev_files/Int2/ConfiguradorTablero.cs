namespace Monopoly
{
    public class ConfiguradorTablero
    {
        public Tablero CrearTablero()
        {
        Tablero tablero = new Tablero();

        tablero.AgregarCasilla(new CasillaEspecial(1, "Inicio", "Salida")); //1

        tablero.AgregarCasilla(new Propiedad(2, "Avenida Central", 200000, 25000)); //2

        tablero.AgregarCasilla(new CasillaEvento(3, "Evento 1", "Recibe un beneficio")); //3

        tablero.AgregarCasilla(new Propiedad(4, "Avenida Norte", 220000, 28000)); //4

        tablero.AgregarCasilla(new CasillaEspecial(5, "Impuesto", "Pago")); //5

        tablero.AgregarCasilla(new Propiedad(6, "Calle del Sol", 250000, 30000)); //6

        tablero.AgregarCasilla(new CasillaEvento(7, "Evento 2", "Pierde dinero")); //7

        tablero.AgregarCasilla(new Propiedad(8, "Avenida del Parque", 280000, 35000)); //8

        tablero.AgregarCasilla(new CasillaEspecial(9, "Descanso", "Especial")); //9

        tablero.AgregarCasilla(new Propiedad(10, "Calle Central", 300000, 38000)); //10

        tablero.AgregarCasilla(new CasillaEvento(11, "Evento 3", "Avanza algunas casillas")); //11

        tablero.AgregarCasilla(new Propiedad(12, "Avenida Este", 320000, 40000)); //12

        tablero.AgregarCasilla(new CasillaEspecial(13, "Visita", "Especial")); //13

        tablero.AgregarCasilla(new Propiedad(14, "Calle Verde", 350000, 45000)); //14

        tablero.AgregarCasilla(new CasillaEvento(15, "Evento 4", "Retrocede algunas casillas")); //15

        tablero.AgregarCasilla(new Propiedad(16, "Avenida del Lago", 380000, 48000)); //16

        tablero.AgregarCasilla(new CasillaEspecial(17, "Impuesto Especial", "Pago")); //17

        tablero.AgregarCasilla(new Propiedad(18, "Calle Principal", 400000, 50000)); //18

        tablero.AgregarCasilla(new CasillaEvento(19, "Evento 5", "Pierde un Turno")); //19

        tablero.AgregarCasilla(new Propiedad(20, "Avenida Sur", 420000, 55000)); //20

        tablero.AgregarCasilla(new CasillaEspecial(21, "Estacionamiento", "Especial")); //21

        tablero.AgregarCasilla(new Propiedad(22, "Calle del Bosque", 450000, 58000)); //22

        tablero.AgregarCasilla(new CasillaEvento(23, "Evento 6", "Recibe dinero")); //23

        tablero.AgregarCasilla(new Propiedad(24, "Avenida del Mar", 500000, 65000)); //24

        return tablero;

        }
    }
}