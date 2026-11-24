using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Auth;

public sealed class PerfilApiAdapter(HttpClient httpClient) : IPerfilApiAdapter
{
    private const string PasswordActualIncorrectaDelServicio = "La contraseña actual es incorrecta.";

    public async Task<Result> CambiarPassword(CambiarPasswordRequest request)
    {
        try
        {
            using var response = await httpClient.PutAsJsonAsync("api/usuarios/perfil/password", request);
            return response.IsSuccessStatusCode
                ? Result.Ok()
                : Result.Fail(response.StatusCode switch
                {
                    HttpStatusCode.BadRequest => Normalizar(await ReadApiMessage(response))
                        ?? "No se pudo cambiar la contraseña. Revise los datos ingresados.",
                    HttpStatusCode.Unauthorized => "No se pudo autorizar el cambio de contraseña. Verifique su sesión.",
                    HttpStatusCode.Forbidden => MensajesAuth.SinPermisos,
                    HttpStatusCode.ServiceUnavailable => MensajesAuth.ServicioNoDisponible,
                    _ => MensajesAuth.ErrorInesperado
                });
        }
        catch (HttpRequestException)
        {
            return Result.Fail(MensajesAuth.SinConexion);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("El servidor tardó demasiado en responder. Intente nuevamente.");
        }
    }

    private static string? Normalizar(string? mensaje) =>
        string.Equals(mensaje, PasswordActualIncorrectaDelServicio, StringComparison.Ordinal)
            ? MensajesAuth.PasswordActualIncorrecta
            : mensaje;

    private static async Task<string?> ReadApiMessage(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorApi>();
            return string.IsNullOrWhiteSpace(error?.Error) ? null : error.Error;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return null;
        }
    }
}
