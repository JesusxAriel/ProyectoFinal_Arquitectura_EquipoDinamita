namespace PuntoMarket.Frontend.Catalogos;

// HttpClient tipado registrado con AddGatewayAdapter: BearerTokenHandler adjunta el JWT de sesión.
public sealed class ClientesApiClient(HttpClient http) : IClientesApiClient
{
    public Task<ResultadoOperacion<PaginaClientesVm>> ListarAsync(ConsultaClientesVm q, CancellationToken ct) =>
        EnviarAsync<PaginaClientesVm>(HttpMethod.Get, $"api/clientes?busqueda={Uri.EscapeDataString(q.Busqueda ?? "")}&estado={Uri.EscapeDataString(q.Estado)}&ordenarPor={Uri.EscapeDataString(q.OrdenarPor)}&descendente={q.Descendente.ToString().ToLowerInvariant()}&pagina={q.Pagina}&tamanoPagina={q.TamanoPagina}", null, ct);
    public Task<ResultadoOperacion<ClienteVm>> ObtenerAsync(int id, CancellationToken ct) => EnviarAsync<ClienteVm>(HttpMethod.Get, $"api/clientes/{id}", null, ct);
    public Task<ResultadoOperacion<ClienteVm>> CrearAsync(ClienteFormVm f, CancellationToken ct) => EnviarAsync<ClienteVm>(HttpMethod.Post, "api/clientes", f, ct);
    public Task<ResultadoOperacion<ClienteVm>> ActualizarAsync(int id, ClienteFormVm f, CancellationToken ct) => EnviarAsync<ClienteVm>(HttpMethod.Put, $"api/clientes/{id}", f, ct);
    public Task<ResultadoOperacion<object>> DesactivarAsync(int id, CancellationToken ct) => EnviarAsync<object>(HttpMethod.Delete, $"api/clientes/{id}", null, ct);
    public Task<ResultadoOperacion<object>> ReactivarAsync(int id, CancellationToken ct) => EnviarAsync<object>(HttpMethod.Patch, $"api/clientes/{id}/reactivar", null, ct);

    private Task<ResultadoOperacion<T>> EnviarAsync<T>(HttpMethod metodo, string ruta, object? cuerpo, CancellationToken ct) =>
        RespuestaGateway.Enviar<T>(http, metodo, ruta, cuerpo, ct);
}
