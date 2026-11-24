using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Auth;

public sealed class ActivacionApiAdapter(HttpClient httpClient) : IActivacionApiAdapter
{
    private const string Ruta = "api/activacion";

    public async Task<Result> Activar(
        string token,
        string nuevaContrasena,
        string confirmarContrasena,
        CancellationToken cancellationToken = default)
    {
        var cuerpo = new { token, nuevaContrasena, confirmarContrasena };

        try
        {
            using var respuesta = await httpClient.PostAsJsonAsync(Ruta, cuerpo, cancellationToken);

            if (respuesta.StatusCode == HttpStatusCode.OK)
                return await LeerActivada(respuesta, cancellationToken);

            return Result.Fail(await MensajeDeError(respuesta, cancellationToken));
        }
        catch (HttpRequestException)
        {
            return Result.Fail(MensajesAuth.SinConexion);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Fail(MensajesAuth.SinConexion);
        }
    }

    private static async Task<Result> LeerActivada(HttpResponseMessage respuesta, CancellationToken cancellationToken)
    {
        try
        {
            var datos = await respuesta.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            return datos.ValueKind == JsonValueKind.Object
                && datos.TryGetProperty("activada", out var activada)
                && activada.ValueKind == JsonValueKind.True
                    ? Result.Ok()
                    : Result.Fail(MensajesAuth.ErrorInesperado);
        }
        catch (Exception excepcion) when (excepcion is JsonException or NotSupportedException)
        {
            return Result.Fail(MensajesAuth.ErrorInesperado);
        }
    }

    private static async Task<string> MensajeDeError(HttpResponseMessage respuesta, CancellationToken cancellationToken)
    {
        var mensajeApi = await LeerMensaje(respuesta, cancellationToken);

        return respuesta.StatusCode switch
        {
            HttpStatusCode.BadRequest => mensajeApi ?? MensajesAuth.SolicitudInvalida,
            HttpStatusCode.ServiceUnavailable => mensajeApi ?? MensajesAuth.ServicioNoDisponible,
            _ => MensajesAuth.ErrorInesperado
        };
    }

    private static async Task<string?> LeerMensaje(HttpResponseMessage respuesta, CancellationToken cancellationToken)
    {
        try
        {
            var error = await respuesta.Content.ReadFromJsonAsync<ErrorApi>(cancellationToken);
            return string.IsNullOrWhiteSpace(error?.Error) ? null : error.Error;
        }
        catch (Exception excepcion) when (excepcion is JsonException or NotSupportedException)
        {
            return null;
        }
    }
}
