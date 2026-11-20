using System.Text;
using System.Text.Json;

namespace PuntoMarket.Frontend.Clientes;

public interface IClientesApiAdapter
{
    Task<ResultadoCliente<PaginaClientes>> ListarAsync(string? busqueda, string estado, string ordenarPor,
        bool descendente, int pagina, int tamanoPagina, CancellationToken ct);
    Task<ResultadoCliente<ClienteFrontend>> ObtenerAsync(int id, CancellationToken ct);
    Task<ResultadoCliente<ClienteFrontend>> CrearAsync(GuardarClienteFrontend entrada, CancellationToken ct);
    Task<ResultadoCliente<ClienteFrontend>> ActualizarAsync(int id, GuardarClienteFrontend entrada, CancellationToken ct);
    Task<ResultadoCliente<object?>> DesactivarAsync(int id, CancellationToken ct);
    Task<ResultadoCliente<ClienteFrontend>> ReactivarAsync(int id, CancellationToken ct);
}

// PATRÓN DE DISEÑO ADAPTER del frontend
public sealed class ClientesApiAdapter(HttpClient http) : IClientesApiAdapter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<ResultadoCliente<PaginaClientes>> ListarAsync(string? busqueda, string estado, string ordenarPor,
        bool descendente, int pagina, int tamanoPagina, CancellationToken ct)
    {
        var query = $"api/clientes?busqueda={Uri.EscapeDataString(busqueda ?? "")}&estado={Uri.EscapeDataString(estado)}&ordenarPor={Uri.EscapeDataString(ordenarPor)}&descendente={descendente.ToString().ToLowerInvariant()}&pagina={pagina}&tamanoPagina={tamanoPagina}";
        return LeerAsync<PaginaClientes>(HttpMethod.Get, query, null, ct);
    }
    public Task<ResultadoCliente<ClienteFrontend>> ObtenerAsync(int id, CancellationToken ct) =>
        LeerAsync<ClienteFrontend>(HttpMethod.Get, $"api/clientes/{id}", null, ct);
    public Task<ResultadoCliente<ClienteFrontend>> CrearAsync(GuardarClienteFrontend entrada, CancellationToken ct) =>
        LeerAsync<ClienteFrontend>(HttpMethod.Post, "api/clientes", entrada, ct);
    public Task<ResultadoCliente<ClienteFrontend>> ActualizarAsync(int id, GuardarClienteFrontend entrada, CancellationToken ct) =>
        LeerAsync<ClienteFrontend>(HttpMethod.Put, $"api/clientes/{id}", entrada, ct);
    public async Task<ResultadoCliente<object?>> DesactivarAsync(int id, CancellationToken ct)
    {
        var respuesta = await http.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"api/clientes/{id}"), ct);
        return respuesta.IsSuccessStatusCode
            ? ResultadoCliente<object?>.Ok(null)
            : await FalloAsync<object?>(respuesta, ct);
    }
    public Task<ResultadoCliente<ClienteFrontend>> ReactivarAsync(int id, CancellationToken ct) =>
        LeerAsync<ClienteFrontend>(HttpMethod.Patch, $"api/clientes/{id}/reactivar", null, ct);

    private async Task<ResultadoCliente<T>> LeerAsync<T>(HttpMethod metodo, string ruta, object? cuerpo, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(metodo, ruta);
        if (cuerpo is not null) request.Content = new StringContent(JsonSerializer.Serialize(cuerpo, Json), Encoding.UTF8, "application/json");
        var respuesta = await http.SendAsync(request, ct);
        if (respuesta.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(object)) return ResultadoCliente<T>.Ok(default!);
            var texto = await respuesta.Content.ReadAsStringAsync(ct);
            return ResultadoCliente<T>.Ok(string.IsNullOrWhiteSpace(texto) ? default! : JsonSerializer.Deserialize<T>(texto, Json)!);
        }
        return await FalloAsync<T>(respuesta, ct);
    }

    private static async Task<ResultadoCliente<T>> FalloAsync<T>(HttpResponseMessage respuesta, CancellationToken ct)
    {
        var problema = await respuesta.Content.ReadAsStringAsync(ct);
        string? codigo = null, mensaje = null;
        IReadOnlyDictionary<string, string[]>? errores = null;
        try
        {
            using var documento = JsonDocument.Parse(problema);
            var root = documento.RootElement;
            if (root.TryGetProperty("code", out var c)) codigo = c.GetString();
            if (root.TryGetProperty("detail", out var d)) mensaje = d.GetString();
            if (root.TryGetProperty("errors", out var e))
                errores = e.EnumerateObject().ToDictionary(x => x.Name,
                    x => x.Value.EnumerateArray().Select(v => v.GetString() ?? "").ToArray());
        }
        catch { }
        return ResultadoCliente<T>.Fallo(codigo, mensaje, (int)respuesta.StatusCode, errores);
    }
}
