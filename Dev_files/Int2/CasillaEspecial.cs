namespace Monopoly
{
    public class CasillaEspecial : Casilla
    {
        public string Tipo {get; set;}

        public CasillaEspecial(int id, string nombre, string tipo) : base(id, nombre)
        {
            Tipo = tipo;
        }

        public override string ObtenerInformacion()
        {
            return $"{Nombre} - Tipo: {Tipo}";
        }
    }
}