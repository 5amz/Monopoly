namespace Monopoly.Administracion;

/// <summary>Comandos que el protocolo del proyecto reconoce.</summary>
public enum ComandoProtocolo
{
    CONECTAR,
    TIRAR_DADOS,
    COMPRAR_PROPIEDAD,
    NO_COMPRAR,
    TERMINAR_TURNO,
    CONSULTAR_ESTADO,
    CONSULTAR_TRANSACCIONES
}
