namespace Monopoly
{
    public class CartaEvento
    {
        public int Id {get; set;}
        public string Descripcion {get; set;}
        public string Tipo {get; set;}
        public int Valor {get; set;}

        public CartaEvento(int id, string descripcion, string tipo, int valor)
        {
            Id = id;
            Descripcion = descripcion;
            Tipo = tipo;
            Valor = valor;
        }
    }
}