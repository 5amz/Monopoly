namespace Monopoly
{
    public class Propiedad : Casilla
    {
        public decimal Precio {get; set;}
        public decimal Alquiler {get; set;}
        public bool Disponible {get; set;}

        public Propiedad(int id, string nombre, decimal precio, decimal alquiler) : base(id, nombre)
        {
            Precio = precio;
            Alquiler = alquiler;
            Disponible = true;
        }

        public override string ObtenerInformacion()
        {
            return $"{Nombre} - Precio: {Precio} - Alquiler: {Alquiler}";
        }
    }
}