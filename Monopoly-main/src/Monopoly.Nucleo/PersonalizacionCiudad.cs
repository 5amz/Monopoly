using System;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;
public static class PersonalizacionCiudad
{
    public const string Titulo = "Ciudad de Canciones";
    
    
// Ejecuta TextoConCanciones.
    public static string TextoConCanciones(string texto, ListaCircularDoble<CasillaVista> casillas)
    {
        foreach (var casilla in casillas)
            if (casilla.Tipo == "PROPIEDAD")
                texto = texto.Replace(casilla.Nombre, casilla.Cancion, StringComparison.Ordinal);
        return texto;
    }

    
// Ejecuta Cancion.
    public static string Cancion(int id)
    {
        return id switch
        {
            1 => "Start Me Up",
            2 => "Tornado of Souls",
            4 => "Holy Diver",
            5 => "Taxman",
            6 => "Hotel California",
            8 => "Welcome to the Jungle",
            9 => "The Trooper",
            10 => "SICKO MODE",
            12 => "Alright",
            13 => "Through the Never",
            14 => "Rapp Snitch Knishes",
            16 => "Flashing Lights",
            17 => "Bulls on Parade",
            18 => "Idol",
            20 => "Hana ni Natte",
            21 => "Highway to Hell",
            22 => "Last Surprise",
            24 => "Gimme Chocolate!!",
            _ => "Discos sorpresa"
        };
    }

    
// Ejecuta Artista.
    public static string Artista(int id)
    {
        return id switch
        {
            1 => "The Rolling Stones",
            2 => "Megadeth",
            9 => "Iron Maiden",
            13 => "Metallica",
            4 => "Dio",
            5 => "The Beatles",
            6 => "Eagles",
            8 => "Guns N' Roses",
            10 => "Travis Scott",
            12 => "Kendrick Lamar",
            14 => "MF DOOM feat. Mr. Fantastik",
            16 => "Kanye West",
            17 => "Rage Against the Machine",
            18 => "YOASOBI",
            20 => "Ryokuoushoku Shakai",
            21 => "AC/DC",
            22 => "Lyn",
            24 => "BABYMETAL",
            _ => "Puesto de discos misteriosos"
        };
    }

    
// Ejecuta CancionEvento.
    public static string CancionEvento(string efecto)
    {
        return efecto switch
        {
            "GanarDinero" or "ABONAR" => "Popular - The Weeknd, Madonna y Playboi Carti",
            "PerderDinero" or "COBRAR" => "For Whom the Bell Tolls - Metallica",
            "Avanzar" or "AVANZAR" => "Run to the Hills - Iron Maiden",
            "Retroceder" => "Back in Black - AC/DC",
            "PerderTurno" => "Boulevard of Broken Dreams - Green Day",
            "IrACasilla" => "Highway Star - Deep Purple",
            _ => "Carta de evento"
        };
    }

    
// Ejecuta ImagenEvento.
    public static string ImagenEvento(string efecto)
    {
        return efecto switch
        {
            "GanarDinero" or "ABONAR" => "carta_ganar",
            "PerderDinero" or "COBRAR" => "carta_perder",
            "Avanzar" or "AVANZAR" => "carta_avanzar",
            "Retroceder" => "carta_retroceder",
            "PerderTurno" => "carta_turno",
            "IrACasilla" => "carta_viajar",
            _ => ""
        };
    }
}
