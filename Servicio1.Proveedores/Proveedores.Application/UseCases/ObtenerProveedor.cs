using Proveedores.Application.Abstractions;
using Proveedores.Application.Dtos;
using Proveedores.Domain.Common;
using Proveedores.Domain.Errors;

namespace Proveedores.Application.UseCases;

public interface IObtenerProveedor
{
    Task<Result<ProveedorDto>> EjecutarAsync(string id, CancellationToken cancellationToken);
}

public sealed class ObtenerProveedor(IProveedorRepository repositorio) : IObtenerProveedor
{
    public async Task<Result<ProveedorDto>> EjecutarAsync(string id, CancellationToken cancellationToken)
    {
        var proveedor = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        return proveedor is null
            ? Result.Failure<ProveedorDto>(ProveedorErrors.NoEncontrado)
            : Result.Success(proveedor.ToDto());
    }
}
