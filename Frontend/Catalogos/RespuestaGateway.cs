using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PuntoMarket.Frontend.Catalogos;

internal static class RespuestaGateway
{
    public static async Task<ResultadoOperacion<T>> Enviar<T>(HttpClient http, HttpMethod metodo,
        string ruta, object? cuerpo, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(metodo, ruta);
            if (cuerpo is not null) request.Content = JsonContent.Create(cuerpo);
            using var response = await http.SendAsync(request, ct);
            var status = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent) return new(true, status, default, null);
                var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                return data is null ? new(false, 502, default, "El servicio devolvió una respuesta vacía.") : new(true, status, data, null);
            }
            var mensaje = status switch
            {
                401 => "La sesión no permite continuar. Vuelva a iniciar sesión.",
                403 => "No tiene permiso para realizar esta operación.",
                404 => "No se encontró el registro solicitado.",
                409 => "La operación entra en conflicto con el estado actual del registro.",
                >= 500 => "El servicio no está disponible. Inténtelo de nuevo más tarde.",
                _ => "Revise los datos enviados."
            };
            string? codigo = null;
            IReadOnlyDictionary<string, string[]>? campos = null;
            try
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                var error = doc.RootElement;
                if (error.ValueKind == JsonValueKind.Object)
                {
                    if (error.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String) codigo = code.GetString();
                    if (status < 500 && status is not (401 or 403) && error.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                        mensaje = detail.GetString() ?? mensaje;
                    if (status < 500 && error.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                        campos = errors.Deserialize<Dictionary<string, string[]>>();
                }
            }
            catch (JsonException) { /* Una respuesta inesperada nunca se presenta como HTML. */ }
            return new(false, status, default, mensaje, campos, codigo);
        }
        catch (JsonException) { return new(false, 502, default, "El servicio devolvió una respuesta inválida."); }
        catch (HttpRequestException) { return new(false, 503, default, "No se pudo conectar con el Gateway."); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { return new(false, 504, default, "El servicio tardó demasiado en responder."); }
    }
}
