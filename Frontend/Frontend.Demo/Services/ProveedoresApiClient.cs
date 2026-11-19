using System.Net;
using System.Text.Json;
using Frontend.Demo.Models;

namespace Frontend.Demo.Services;

// =====================================================
// INTEGRACIÓN GRUPO 2 - COMUNICACIÓN FRONTEND -> GATEWAY
// ESTADO: CONSERVAR (cliente tipado reutilizable por el frontend definitivo)
//
// QUÉ HACE: llama a /api/proveedores... SOLO a través del API Gateway (cliente "ApiGateway") y envía el
//   JWT en el encabezado Authorization: Bearer. Traduce errores HTTP a mensajes en español.
// QUÉ DEBE MODIFICAR EL GRUPO 2: sustituir DemoTokenProvider por el token del usuario real.
// QUÉ NO DEBE MODIFICARSE: las rutas ni los nombres de los parámetros de consulta (contrato de la API).
// =====================================================
// INTEGRACIÓN: Referencia para un cliente separado por módulo, no un cliente genérico de todos los CRUD.
//   Reutilizar IHttpClientFactory y el origen de identidad acordado; no asumir estos DTOs, rutas ni permisos
//   para Usuarios. EnviarAsync es privado y los mensajes de error también son específicos de Proveedores.
public sealed class ProveedoresApiClient(IHttpClientFactory fabrica, DemoPerfilSession perfil, DemoTokenProvider tokens)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<ApiResult<PaginaVm>> ListarAsync(ConsultaVm c, CancellationToken ct)
    {
        var url = $"api/proveedores?busqueda={Uri.EscapeDataString(c.Busqueda ?? string.Empty)}" +
                  $"&estado={Uri.EscapeDataString(c.Estado)}&ordenarPor={Uri.EscapeDataString(c.OrdenarPor)}&descendente={c.Descendente.ToString().ToLowerInvariant()}" +
                  $"&pagina={c.Pagina}&tamanoPagina={c.TamanoPagina}";
        return EnviarAsync<PaginaVm>(HttpMethod.Get, url, null, ct);
    }

    // MongoDB filtra y ordena antes de paginar; el frontend no descarga la colección completa.
    public Task<ApiResult<PaginaVm>> BuscarAsync(ConsultaVm c, CancellationToken ct) => ListarAsync(c, ct);

    public Task<ApiResult<ProveedorVm>> ObtenerAsync(string id, CancellationToken ct) =>
        EnviarAsync<ProveedorVm>(HttpMethod.Get, $"api/proveedores/{Uri.EscapeDataString(id)}", null, ct);

    public Task<ApiResult<ProveedorVm>> CrearAsync(ProveedorFormVm form, CancellationToken ct) =>
        EnviarAsync<ProveedorVm>(HttpMethod.Post, "api/proveedores", form, ct);

    public Task<ApiResult<ProveedorVm>> ActualizarAsync(string id, ProveedorFormVm form, CancellationToken ct) =>
        EnviarAsync<ProveedorVm>(HttpMethod.Put, $"api/proveedores/{Uri.EscapeDataString(id)}", form, ct);

    public Task<ApiResult<object>> DesactivarAsync(string id, CancellationToken ct) =>
        EnviarAsync<object>(HttpMethod.Delete, $"api/proveedores/{Uri.EscapeDataString(id)}", null, ct);

    public Task<ApiResult<ProveedorVm>> ReactivarAsync(string id, CancellationToken ct) =>
        EnviarAsync<ProveedorVm>(HttpMethod.Patch, $"api/proveedores/{Uri.EscapeDataString(id)}/reactivar", null, ct);

    private async Task<ApiResult<T>> EnviarAsync<T>(HttpMethod metodo, string url, object? cuerpo, CancellationToken ct)
    {
        try
        {
            var perfilSolicitud = perfil.Perfil;
            var token = await tokens.ObtenerAsync(perfilSolicitud, ct);
            if (token is null)
                return new ApiResult<T>(false, 503, default, "No se pudo obtener el token de demostración. Verifique que la identidad de desarrollo y el Gateway estén activos.");

            for (var intento = 0; intento < 2; intento++)
            {
                using var solicitud = new HttpRequestMessage(metodo, url);
                // INTEGRACIÓN: El Bearer se añade en el servidor Razor, no en localStorage ni en una URL.
                //   El Gateway lo propaga y la API comprueba su firma, audiencia y permisos.
                solicitud.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                if (cuerpo is not null) solicitud.Content = JsonContent.Create(cuerpo, options: Json);

                using var http = fabrica.CreateClient("ApiGateway");
                using var respuesta = await http.SendAsync(solicitud, ct);
                var estado = (int)respuesta.StatusCode;

                if (respuesta.StatusCode == HttpStatusCode.Unauthorized && intento == 0)
                {
                    // Solo se repite un 401, rechazado antes del caso de uso.
                    respuesta.Dispose();
                    token = await tokens.RenovarAsync(perfilSolicitud, token, ct);
                    if (token is null)
                        return new ApiResult<T>(false, 503, default, "No se pudo renovar el token de demostración.");
                    continue;
                }
                if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
                    await tokens.InvalidarAsync(perfilSolicitud, token, ct);

                if (respuesta.IsSuccessStatusCode)
                {
                    if (respuesta.StatusCode == HttpStatusCode.NoContent || typeof(T) == typeof(object))
                        return new ApiResult<T>(true, estado, default, null);
                    var datos = await respuesta.Content.ReadFromJsonAsync<T>(Json, ct);
                    return new ApiResult<T>(true, estado, datos, null);
                }

                var mensaje = await MensajeDeErrorAsync(respuesta, ct);
                IReadOnlyDictionary<string, string[]>? campos = null;
                try
                {
                    using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
                    if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                        campos = errors.Deserialize<Dictionary<string, string[]>>(Json);
                }
                catch (JsonException) { /* Se conserva el mensaje controlado. */ }
                return new ApiResult<T>(false, estado, default, mensaje, campos);
            }
            throw new InvalidOperationException("Se agotaron los intentos de autenticación.");
        }
        catch (JsonException)
        {
            return new ApiResult<T>(false, 502, default, "El servicio devolvió una respuesta JSON inválida.");
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>(false, 503, default, "No se pudo conectar con el API Gateway.");
        }
        catch (TaskCanceledException)
        {
            return new ApiResult<T>(false, 504, default, "El servicio tardó demasiado en responder.");
        }
    }

    private static async Task<string> MensajeDeErrorAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        switch (respuesta.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                return "No autenticado: el token es inválido o ha expirado.";
            case HttpStatusCode.Forbidden:
                return "No tiene permisos para realizar esta acción. El perfil Cajero solo puede consultar.";
        }

        try
        {
            using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("detail", out var detalle) && detalle.GetString() is { Length: > 0 } texto)
                return texto;
        }
        catch (JsonException) { /* cuerpo no JSON: se usa el mensaje genérico */ }

        return $"La operación no pudo completarse (código {(int)respuesta.StatusCode}).";
    }
}
