namespace PuntoMarket.Frontend.Catalogos;

public interface IProveedoresApiClient
{
    Task<ResultadoOperacion<PaginaVm>> ListarAsync(ConsultaVm c, CancellationToken ct);
    Task<ResultadoOperacion<PaginaVm>> BuscarAsync(ConsultaVm c, CancellationToken ct);
    Task<ResultadoOperacion<ProveedorVm>> ObtenerAsync(string id, CancellationToken ct);
    Task<ResultadoOperacion<ProveedorVm>> CrearAsync(ProveedorFormVm f, CancellationToken ct);
    Task<ResultadoOperacion<ProveedorVm>> ActualizarAsync(string id, ProveedorFormVm f, CancellationToken ct);
    Task<ResultadoOperacion<object>> DesactivarAsync(string id, CancellationToken ct);
    Task<ResultadoOperacion<ProveedorVm>> ReactivarAsync(string id, CancellationToken ct);
}
