using PuntoMarket.Productos.Domain;
using PuntoMarket.Productos.Application.Ports.In;
using PuntoMarket.Productos.Application.Ports.Out;
namespace PuntoMarket.Productos.Application.UseCases;

public sealed class ListarProductos(IProductoRepository repo) : IListarProductos
{
    public Task<Result<PaginaResultado<Producto>>> EjecutarAsync(ListarProductosQuery q, CancellationToken ct) {
        var r = q.Normalizar();
        return r.IsFailure ? Task.FromResult(Result.Failure<PaginaResultado<Producto>>(r.Error)) : repo.ListarAsync(r.Value, ct);
    }
}
public sealed class ObtenerProducto(IProductoRepository repo) : IObtenerProducto
{
    public Task<Result<Producto>> EjecutarAsync(int id, CancellationToken ct) => id <= 0
        ? Task.FromResult(Result.Failure<Producto>(Error.NotFound("Producto.NoEncontrado", "El producto no existe.")))
        : repo.ObtenerAsync(id, ct);
}
public sealed class ConsultarCatalogos(IProductoRepository repo) : IConsultarCatalogos
{
    public Task<Result<CatalogosDto>> EjecutarAsync(CancellationToken ct) => repo.CatalogosAsync(ct);
}
public sealed class CrearProducto(IProductoRepository repo, IProveedorApiClient proveedores, ICurrentUser usuario) : ICrearProducto
{
    public async Task<Result<Producto>> EjecutarAsync(DatosProducto datos, CancellationToken ct) {
        if (string.IsNullOrWhiteSpace(usuario.UserId)) return Result.Failure<Producto>(Error.Unauthorized("Usuario.NoAutenticado", "Se requiere una identidad autenticada."));
        var d = datos.Normalizar(); var r = ProductoReglas.Validar(d);
        if (r.IsFailure) return Result.Failure<Producto>(r.Error);
        var proveedor = await proveedores.ValidarActivoAsync(d.ProveedorId!, ct);
        return proveedor.IsFailure ? Result.Failure<Producto>(proveedor.Error) : await repo.CrearAsync(d, usuario.UserId, ct);
    }
}
public sealed class ActualizarProducto(IProductoRepository repo, IProveedorApiClient proveedores, ICurrentUser usuario) : IActualizarProducto
{
    public async Task<Result<Producto>> EjecutarAsync(int id, DatosProducto datos, CancellationToken ct) {
        if (string.IsNullOrWhiteSpace(usuario.UserId)) return Result.Failure<Producto>(Error.Unauthorized("Usuario.NoAutenticado", "Se requiere una identidad autenticada."));
        var d = datos.Normalizar(); var validacion = ProductoReglas.Validar(d);
        if (validacion.IsFailure) return Result.Failure<Producto>(validacion.Error);
        var anterior = await repo.ObtenerAsync(id, ct);
        if (anterior.IsFailure) return anterior;
        if (!anterior.Value.EstaActivo) return Result.Failure<Producto>(Error.Conflict("Producto.Inactivo", "No se puede editar un producto desactivado."));
        var reglas = ProductoReglas.Validar(d, anterior.Value.PrecioVenta);
        if (reglas.IsFailure) return Result.Failure<Producto>(reglas.Error);
        var proveedor = await proveedores.ValidarActivoAsync(d.ProveedorId!, ct);
        return proveedor.IsFailure ? Result.Failure<Producto>(proveedor.Error) : await repo.ActualizarAsync(id, d, usuario.UserId, ct);
    }
}
public sealed class DesactivarProducto(IProductoRepository repo, ICurrentUser usuario) : IDesactivarProducto
{
    public Task<Result> EjecutarAsync(int id, CancellationToken ct) => string.IsNullOrWhiteSpace(usuario.UserId)
        ? Task.FromResult(Result.Failure(Error.Unauthorized("Usuario.NoAutenticado", "Se requiere una identidad autenticada.")))
        : repo.DesactivarAsync(id, usuario.UserId, ct);
}
