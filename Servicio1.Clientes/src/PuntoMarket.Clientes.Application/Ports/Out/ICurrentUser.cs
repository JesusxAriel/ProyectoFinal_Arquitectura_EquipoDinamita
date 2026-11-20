namespace PuntoMarket.Clientes.Application.Ports.Out;

/// <summary>
/// PUERTO DE SALIDA: identidad del usuario autenticado, usada para la auditoría.
/// La implementación vive en el adaptador de entrada (Adapters.In.Api/Security) y lee el claim "sub" del JWT.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Identificador del usuario (claim "sub", texto). Null si no se pudo determinar.</summary>
    string? UserId { get; }
}
