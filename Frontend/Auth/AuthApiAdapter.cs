using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Auth;

public sealed class AuthApiAdapter(HttpClient httpClient) : IAuthApiAdapter
{
    public const string LoginRoute = "api/auth/login";
    private const string LogoutRoute = "api/auth/logout";

    public async Task<Result<LoginResponse>> Login(LoginRequest request)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(LoginRoute, request);
            return response.IsSuccessStatusCode
                ? await ReadLogin(response)
                : Result<LoginResponse>.Fail(await ErrorMessage(response));
        }
        catch (Exception exception) when (IsTransportFailure(exception))
        {
            return Result<LoginResponse>.Fail(MensajesAuth.SinConexion);
        }
    }

    public async Task<Result> Logout()
    {
        try
        {
            using var response = await httpClient.PostAsync(LogoutRoute, null);
            return response.IsSuccessStatusCode
                ? Result.Ok()
                : Result.Fail(await ErrorMessage(response));
        }
        catch (Exception exception) when (IsTransportFailure(exception))
        {
            return Result.Fail(MensajesAuth.SinConexion);
        }
    }

    private static async Task<Result<LoginResponse>> ReadLogin(HttpResponseMessage response)
    {
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return login is null || string.IsNullOrEmpty(login.AccessToken)
            ? Result<LoginResponse>.Fail(MensajesAuth.ErrorInesperado)
            : Result<LoginResponse>.Ok(login);
    }

    private static async Task<string> ErrorMessage(HttpResponseMessage response)
    {
        var apiMessage = await ReadApiMessage(response);
        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => apiMessage ?? MensajesAuth.SolicitudInvalida,
            HttpStatusCode.Unauthorized => apiMessage ?? MensajesAuth.CredencialesIncorrectas,
            HttpStatusCode.Forbidden => MensajesAuth.SinPermisos,
            HttpStatusCode.ServiceUnavailable => apiMessage ?? MensajesAuth.ServicioNoDisponible,
            _ => MensajesAuth.ErrorInesperado
        };
    }

    private static async Task<string?> ReadApiMessage(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorApi>();
            return string.IsNullOrWhiteSpace(error?.Error) ? null : error.Error;
        }
        catch (Exception exception) when (IsTransportFailure(exception))
        {
            return null;
        }
    }

    private static bool IsTransportFailure(Exception exception) =>
        exception is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException;
}
