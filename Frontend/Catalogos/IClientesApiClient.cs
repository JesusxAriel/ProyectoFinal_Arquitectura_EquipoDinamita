namespace PuntoMarket.Frontend.Catalogos;

public interface IClientesApiClient
{
    Task<ResultadoOperacion<PaginaClientesVm>> ListarAsync(ConsultaClientesVm q, CancellationToken ct);
    Task<ResultadoOperacion<ClienteVm>> ObtenerAsync(int id, CancellationToken ct);
    Task<ResultadoOperacion<ClienteVm>> CrearAsync(ClienteFormVm f, CancellationToken ct);
    Task<ResultadoOperacion<ClienteVm>> ActualizarAsync(int id, ClienteFormVm f, CancellationToken ct);
    Task<ResultadoOperacion<object>> DesactivarAsync(int id, CancellationToken ct);
    Task<ResultadoOperacion<object>> ReactivarAsync(int id, CancellationToken ct);
}
