using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PuntoMarket.Gateway.Auth;

public sealed class UsuariosClient(HttpClient httpClient, IOptions<UsuariosOptions> options)
{
    private readonly UsuariosOptions _options = options.Value;

    public async Task<ClientOutcome<UsuarioActual>> GetCurrentUser(
        string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _options.CurrentUserPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            return await ReadUser(response, cancellationToken);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return ClientOutcome<UsuarioActual>.Unavailable();
        }
    }

    private static async Task<ClientOutcome<UsuarioActual>> ReadUser(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            return ClientOutcome<UsuarioActual>.Rejected();
        }

        if (!response.IsSuccessStatusCode)
        {
            return ClientOutcome<UsuarioActual>.Unavailable();
        }

        var user = await response.Content.ReadFromJsonAsync<UsuarioActual>(cancellationToken);
        return user is null
            ? ClientOutcome<UsuarioActual>.Unavailable()
            : ClientOutcome<UsuarioActual>.Success(user);
    }
}
