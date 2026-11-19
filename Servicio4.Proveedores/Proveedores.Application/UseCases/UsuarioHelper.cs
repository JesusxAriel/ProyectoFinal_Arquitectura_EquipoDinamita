using Proveedores.Application.Abstractions;
using Proveedores.Domain.Common;
using Proveedores.Domain.Errors;

namespace Proveedores.Application.UseCases;

internal static class UsuarioHelper
{
    /// <summary>Obtiene el id del usuario autenticado o un Failure (401) si el token no lo trae.</summary>
    public static Result<string> ObtenerUsuario(ICurrentUser currentUser) =>
        string.IsNullOrWhiteSpace(currentUser.UserId)
            ? Result.Failure<string>(ProveedorErrors.UsuarioNoIdentificado)
            : Result.Success<string>(currentUser.UserId!);
}
