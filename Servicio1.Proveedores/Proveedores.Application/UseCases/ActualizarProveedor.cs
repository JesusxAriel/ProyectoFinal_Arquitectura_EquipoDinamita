using Proveedores.Application.Abstractions;
using Proveedores.Application.Dtos;
using Proveedores.Domain.Common;
using Proveedores.Domain.Entities;
using Proveedores.Domain.Errors;

namespace Proveedores.Application.UseCases;

public interface IActualizarProveedor
{
    Task<Result<ProveedorDto>> EjecutarAsync(string id, GuardarProveedorRequest request, CancellationToken cancellationToken);
}

public sealed class ActualizarProveedor(IProveedorRepository repositorio, ICurrentUser usuarioActual, TimeProvider reloj) : IActualizarProveedor
{
    public async Task<Result<ProveedorDto>> EjecutarAsync(string id, GuardarProveedorRequest request, CancellationToken cancellationToken)
    {
        var usuario = UsuarioHelper.ObtenerUsuario(usuarioActual);
        if (usuario.IsFailure) return Result.Failure<ProveedorDto>(usuario.Error);

        var proveedor = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (proveedor is null) return Result.Failure<ProveedorDto>(ProveedorErrors.NoEncontrado);

        var datos = new DatosProveedor(request.NombreEmpresa, request.NumeroEmpresa, request.CorreoReferencia, request.EsAutogestionado);
        var cambio = proveedor.Actualizar(datos, usuario.Value, reloj.GetUtcNow().UtcDateTime);
        if (cambio.IsFailure) return Result.Failure<ProveedorDto>(cambio.Error);

        if (await repositorio.ExisteActivoConNombreAsync(proveedor.NombreNormalizado, proveedor.Id, cancellationToken))
            return Result.Failure<ProveedorDto>(ProveedorErrors.NombreDuplicado);

        var guardado = await repositorio.ActualizarAsync(proveedor, cancellationToken);
        return guardado.IsFailure
            ? Result.Failure<ProveedorDto>(guardado.Error)
            : Result.Success(proveedor.ToDto());
    }
}
