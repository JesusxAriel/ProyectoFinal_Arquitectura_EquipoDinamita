using Proveedores.Domain.Common;

namespace Proveedores.Application.Abstractions;

public interface IProductoApiClient
{
    Task<Result<bool>> TieneProductosActivosAsync(string proveedorId, CancellationToken cancellationToken);
}
