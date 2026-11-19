namespace Proveedores.Application.Abstractions;

/// <summary>
/// Identidad del usuario autenticado, usada para la auditoría.
/// La implementación vive en Proveedores.API/Security/HttpContextCurrentUser.cs y lee el claim "sub" del JWT.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Identificador del usuario (claim "sub", cadena). Null si no se pudo determinar.</summary>
    string? UserId { get; }
}
