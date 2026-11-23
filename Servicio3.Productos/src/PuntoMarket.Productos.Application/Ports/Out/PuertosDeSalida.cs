using PuntoMarket.Productos.Domain;
namespace PuntoMarket.Productos.Application.Ports.Out;

public interface ICurrentUser { string? UserId { get; } }
public interface IProveedorApiClient { Task<Result> ValidarActivoAsync(string id, CancellationToken ct); }
public interface IProductoRepository
{
    Task<Result<PaginaResultado<Producto>>> ListarAsync(ListarProductosQuery query, CancellationToken ct);
    Task<Result<Producto>> ObtenerAsync(int id, CancellationToken ct);
    Task<Result<CatalogosDto>> CatalogosAsync(CancellationToken ct);
    Task<Result<Producto>> CrearAsync(DatosProducto datos, string usuario, CancellationToken ct);
    Task<Result<Producto>> ActualizarAsync(int id, DatosProducto datos, string usuario, CancellationToken ct);
    Task<Result> DesactivarAsync(int id, string usuario, CancellationToken ct);
}
// Factory Method: la creación concreta se decide en el adaptador, nunca en Application.
public abstract class ProductoRepositoryCreator
{
    public abstract IProductoRepository Crear();
}
