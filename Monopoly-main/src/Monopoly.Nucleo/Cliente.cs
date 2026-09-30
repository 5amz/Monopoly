using System;
using Monopoly.Protocolo;

namespace Monopoly.Nucleo;


public sealed class Cliente : IDisposable
{
    public ConexionServidor Conexion { get; } = new();
// Ejecuta Conectar.
    public void Conectar(string ip, int puerto) => Conexion.Conectar(ip, puerto);
// Ejecuta Solicitar.
    public bool Solicitar(Mensaje mensaje) => Conexion.Enviar(mensaje);
// Ejecuta Dispose.
    public void Dispose() => Conexion.Dispose();
}
