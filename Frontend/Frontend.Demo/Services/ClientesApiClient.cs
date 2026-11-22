using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Frontend.Demo.Models;

namespace Frontend.Demo.Services;

// DEMO TEMPORAL: retirar perfil/token demo al integrar con Grupo 2. Conservar rutas y DTOs.
// Reutiliza el cliente nombrado y la caché de tokens existentes; las API autorizan cada operación.
public sealed class ClientesApiClient(IHttpClientFactory fabrica, DemoPerfilSession perfil, DemoTokenProvider tokens)
{
    public Task<ApiResult<PaginaClientesVm>> ListarAsync(ConsultaClientesVm q, CancellationToken ct) =>
        EnviarAsync<PaginaClientesVm>(HttpMethod.Get, $"api/clientes?busqueda={Uri.EscapeDataString(q.Busqueda ?? "")}&estado={Uri.EscapeDataString(q.Estado)}&ordenarPor={Uri.EscapeDataString(q.OrdenarPor)}&descendente={q.Descendente.ToString().ToLowerInvariant()}&pagina={q.Pagina}&tamanoPagina={q.TamanoPagina}", null, ct);
    public Task<ApiResult<ClienteVm>> ObtenerAsync(int id, CancellationToken ct) => EnviarAsync<ClienteVm>(HttpMethod.Get, $"api/clientes/{id}", null, ct);
    public Task<ApiResult<ClienteVm>> CrearAsync(ClienteFormVm f, CancellationToken ct) => EnviarAsync<ClienteVm>(HttpMethod.Post, "api/clientes", f, ct);
    public Task<ApiResult<ClienteVm>> ActualizarAsync(int id, ClienteFormVm f, CancellationToken ct) => EnviarAsync<ClienteVm>(HttpMethod.Put, $"api/clientes/{id}", f, ct);
    public Task<ApiResult<object>> DesactivarAsync(int id, CancellationToken ct) => EnviarAsync<object>(HttpMethod.Delete, $"api/clientes/{id}", null, ct);
    public Task<ApiResult<object>> ReactivarAsync(int id, CancellationToken ct) => EnviarAsync<object>(HttpMethod.Patch, $"api/clientes/{id}/reactivar", null, ct);

    private async Task<ApiResult<T>> EnviarAsync<T>(HttpMethod metodo, string ruta, object? cuerpo, CancellationToken ct)
    {
        try
        {
            var identidad = perfil.Perfil;
            var token = await tokens.ObtenerAsync(identidad, ct);
            for (var intento = 0; intento < 2 && token is not null; intento++)
            {
                using var solicitud = new HttpRequestMessage(metodo, ruta);
                solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                if (cuerpo is not null) solicitud.Content = JsonContent.Create(cuerpo);
                using var http = fabrica.CreateClient("ApiGateway");
                using var r = await http.SendAsync(solicitud, ct);
                if (r.StatusCode == HttpStatusCode.Unauthorized && intento == 0)
                { token = await tokens.RenovarAsync(identidad, token, ct); continue; }
                if (r.StatusCode == HttpStatusCode.Unauthorized) await tokens.InvalidarAsync(identidad, token, ct);
                if (r.IsSuccessStatusCode)
                {
                    if (r.StatusCode == HttpStatusCode.NoContent) return new(true, (int)r.StatusCode, default, null);
                    var datos = await r.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                    return datos is null ? new(false, 502, default, "Clientes devolvió una respuesta vacía.") : new(true, (int)r.StatusCode, datos, null);
                }
                string mensaje = r.StatusCode switch { HttpStatusCode.Forbidden => "No tiene permiso para esta operación.", HttpStatusCode.Unauthorized => "La sesión de demostración no es válida. Cambie de perfil e inténtelo de nuevo.", _ => "No se pudo completar la operación de Clientes." };
                IReadOnlyDictionary<string, string[]>? campos = null;
                try
                {
                    using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct));
                    if ((int)r.StatusCode is >= 400 and < 500 && doc.RootElement.TryGetProperty("detail", out var detalle) && detalle.ValueKind == JsonValueKind.String)
                        mensaje = detalle.GetString() ?? mensaje;
                    if ((int)r.StatusCode < 500 && doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                        campos = errors.Deserialize<Dictionary<string, string[]>>();
                }
                catch (JsonException) { /* No mostrar HTML ni detalles técnicos. */ }
                return new(false, (int)r.StatusCode, default, mensaje, campos);
            }
            return new(false, 503, default, "No se pudo obtener el JWT de demostración. Verifique Gateway e identidad.");
        }
        catch (JsonException) { return new(false, 502, default, "Clientes devolvió una respuesta JSON inválida."); }
        catch (HttpRequestException) { return new(false, 503, default, "No se pudo conectar con Clientes mediante el Gateway."); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return new(false, 504, default, "Clientes tardó demasiado en responder."); }
    }
}
