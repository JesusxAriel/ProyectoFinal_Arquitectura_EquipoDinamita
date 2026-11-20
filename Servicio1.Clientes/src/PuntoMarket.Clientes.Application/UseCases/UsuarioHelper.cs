using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Errors;

namespace PuntoMarket.Clientes.Application.UseCases;

internal static class UsuarioHelper
{
    /// <summary>Obtiene el id del usuario autenticado o un Failure (401) si el token no lo trae.</summary>
    public static Result<string> ObtenerUsuario(ICurrentUser usuarioActual) =>
        string.IsNullOrWhiteSpace(usuarioActual.UserId)
            ? Result.Failure<string>(ClienteErrors.UsuarioNoIdentificado)
            : Result.Success(usuarioActual.UserId!);
}
