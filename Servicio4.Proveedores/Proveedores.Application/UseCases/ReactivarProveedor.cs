using Proveedores.Application.Abstractions;
using Proveedores.Application.Dtos;
using Proveedores.Domain.Common;
using Proveedores.Domain.Errors;

namespace Proveedores.Application.UseCases;

public interface IReactivarProveedor
{
    Task<Result<ProveedorDto>> EjecutarAsync(string id, CancellationToken cancellationToken);
}

public sealed class ReactivarProveedor(IProveedorRepository repositorio, ICurrentUser usuarioActual, TimeProvider reloj) : IReactivarProveedor
{
    public async Task<Result<ProveedorDto>> EjecutarAsync(string id, CancellationToken cancellationToken)
    {
        var usuario = UsuarioHelper.ObtenerUsuario(usuarioActual);
        if (usuario.IsFailure) return Result.Failure<ProveedorDto>(usuario.Error);

        var proveedor = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (proveedor is null) return Result.Failure<ProveedorDto>(ProveedorErrors.NoEncontrado);

        if (proveedor.EstaActivo) return Result.Failure<ProveedorDto>(ProveedorErrors.YaActivo);

        // Regla 12: no se puede reactivar si otro proveedor activo ya usa el mismo nombre.
        if (await repositorio.ExisteActivoConNombreAsync(proveedor.NombreNormalizado, proveedor.Id, cancellationToken))
            return Result.Failure<ProveedorDto>(ProveedorErrors.ReactivacionConflictiva);

        var cambio = proveedor.Reactivar(usuario.Value, reloj.GetUtcNow().UtcDateTime);
        if (cambio.IsFailure) return Result.Failure<ProveedorDto>(cambio.Error);

        var guardado = await repositorio.ActualizarAsync(proveedor, cancellationToken);
        if (guardado.IsFailure)
        {
            // Carrera: otro proveedor activo con el mismo nombre se creó entre la comprobación y el guardado.
            var error = guardado.Error == ProveedorErrors.NombreDuplicado ? ProveedorErrors.ReactivacionConflictiva : guardado.Error;
            return Result.Failure<ProveedorDto>(error);
        }
        return Result.Success(proveedor.ToDto());
    }
}
