using PuntoMarket.Productos.Domain;
namespace PuntoMarket.Productos.Application.Ports.In;
public interface IListarProductos { Task<Result<PaginaResultado<Producto>>> EjecutarAsync(ListarProductosQuery query, CancellationToken ct); }
public interface IObtenerProducto { Task<Result<Producto>> EjecutarAsync(int id, CancellationToken ct); }
public interface IConsultarCatalogos { Task<Result<CatalogosDto>> EjecutarAsync(CancellationToken ct); }
public interface ICrearProducto { Task<Result<Producto>> EjecutarAsync(DatosProducto datos, CancellationToken ct); }
public interface IActualizarProducto { Task<Result<Producto>> EjecutarAsync(int id, DatosProducto datos, CancellationToken ct); }
public interface IDesactivarProducto { Task<Result> EjecutarAsync(int id, CancellationToken ct); }
