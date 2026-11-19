using Proveedores.Application.Abstractions;
using Proveedores.Domain.Common;
using Proveedores.Domain.Errors;

namespace Proveedores.Application.UseCases;

public interface IDesactivarProveedor
{
    Task<Result> EjecutarAsync(string id, CancellationToken cancellationToken);
}

/// <summary>
/// Eliminación LÓGICA (estaActivo = false).
/// PENDIENTE DE FUTURA ENTREGA: antes de desactivar debería consultarse IProductoApiClient para impedir
/// desactivar proveedores con productos activos. En esta entrega NO se invoca (los CRUD son independientes).
/// </summary>
public sealed class DesactivarProveedor(IProveedorRepository repositorio, ICurrentUser usuarioActual, TimeProvider reloj) : IDesactivarProveedor
{
    public async Task<Result> EjecutarAsync(string id, CancellationToken cancellationToken)
    {
        var usuario = UsuarioHelper.ObtenerUsuario(usuarioActual);
        if (usuario.IsFailure) return Result.Failure(usuario.Error);

        var proveedor = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (proveedor is null) return Result.Failure(ProveedorErrors.NoEncontrado);

        var cambio = proveedor.Desactivar(usuario.Value, reloj.GetUtcNow().UtcDateTime);
        if (cambio.IsFailure) return cambio;

        return await repositorio.ActualizarAsync(proveedor, cancellationToken);
    }
}
