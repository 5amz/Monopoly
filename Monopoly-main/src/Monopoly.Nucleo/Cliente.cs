using System;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;

/// <summary>Fachada de la rúbrica; toda comunicación usa el transporte oficial.</summary>
public sealed class Cliente : IDisposable
{
    public ConexionServidor Conexion { get; } = new();
    public void Conectar(string ip, int puerto) => Conexion.Conectar(ip, puerto);
    public bool Solicitar(Mensaje mensaje) => Conexion.Enviar(mensaje);
    public void Dispose() => Conexion.Dispose();
}
