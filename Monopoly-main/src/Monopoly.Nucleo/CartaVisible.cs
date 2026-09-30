namespace Monopoly.Nucleo;


public sealed record CartaVisible(string Cancion, string Texto, string Efecto, int Valor)
{
    public string Imagen { get; } = PersonalizacionCiudad.ImagenEvento(Efecto);
    public FragmentoMusical? Musica { get; internal set; }
    internal ConfiguracionMusica? ConfiguracionMusical { get; set; }
}
