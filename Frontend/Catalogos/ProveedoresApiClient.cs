namespace PuntoMarket.Frontend.Catalogos;

// HttpClient tipado registrado con AddGatewayAdapter: BearerTokenHandler adjunta el JWT de sesión.
public sealed class ProveedoresApiClient(HttpClient http) : IProveedoresApiClient
{
    public Task<ResultadoOperacion<PaginaVm>> ListarAsync(ConsultaVm c, CancellationToken ct)
    {
        var url = $"api/proveedores?busqueda={Uri.EscapeDataString(c.Busqueda ?? string.Empty)}" +
                  $"&estado={Uri.EscapeDataString(c.Estado)}&ordenarPor={Uri.EscapeDataString(c.OrdenarPor)}&descendente={c.Descendente.ToString().ToLowerInvariant()}" +
                  $"&pagina={c.Pagina}&tamanoPagina={c.TamanoPagina}";
        return EnviarAsync<PaginaVm>(HttpMethod.Get, url, null, ct);
    }

    // MongoDB filtra y ordena antes de paginar; el frontend no descarga la colección completa.
    public Task<ResultadoOperacion<PaginaVm>> BuscarAsync(ConsultaVm c, CancellationToken ct) => ListarAsync(c, ct);

    public Task<ResultadoOperacion<ProveedorVm>> ObtenerAsync(string id, CancellationToken ct) =>
        EnviarAsync<ProveedorVm>(HttpMethod.Get, $"api/proveedores/{Uri.EscapeDataString(id)}", null, ct);

    public Task<ResultadoOperacion<ProveedorVm>> CrearAsync(ProveedorFormVm form, CancellationToken ct) =>
        EnviarAsync<ProveedorVm>(HttpMethod.Post, "api/proveedores", form, ct);

    public Task<ResultadoOperacion<ProveedorVm>> ActualizarAsync(string id, ProveedorFormVm form, CancellationToken ct) =>
        EnviarAsync<ProveedorVm>(HttpMethod.Put, $"api/proveedores/{Uri.EscapeDataString(id)}", form, ct);

    public Task<ResultadoOperacion<object>> DesactivarAsync(string id, CancellationToken ct) =>
        EnviarAsync<object>(HttpMethod.Delete, $"api/proveedores/{Uri.EscapeDataString(id)}", null, ct);

    public Task<ResultadoOperacion<ProveedorVm>> ReactivarAsync(string id, CancellationToken ct) =>
        EnviarAsync<ProveedorVm>(HttpMethod.Patch, $"api/proveedores/{Uri.EscapeDataString(id)}/reactivar", null, ct);

    private Task<ResultadoOperacion<T>> EnviarAsync<T>(HttpMethod metodo, string ruta, object? cuerpo, CancellationToken ct) =>
        RespuestaGateway.Enviar<T>(http, metodo, ruta, cuerpo, ct);
}
