using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PuntoMarket.Gateway.Auth;

public sealed class KeycloakClient(HttpClient httpClient, IOptions<KeycloakOptions> options)
{
    private readonly KeycloakOptions _options = options.Value;

    public async Task<ClientOutcome<KeycloakToken>> RequestToken(
        string username, string password, CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.ClientId,
            ["username"] = username,
            ["password"] = password
        };

        try
        {
            using var response = await httpClient.PostAsync(
                _options.TokenEndpoint, new FormUrlEncodedContent(form), cancellationToken);
            return await ReadToken(response, cancellationToken);
        }
        catch (Exception exception) when (IsConnectionFailure(exception))
        {
            return ClientOutcome<KeycloakToken>.Unavailable();
        }
    }

    public async Task RevokeRefreshToken(string refreshToken, CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["refresh_token"] = refreshToken
        };

        try
        {
            using var response = await httpClient.PostAsync(
                _options.LogoutEndpoint, new FormUrlEncodedContent(form), cancellationToken);
        }
        catch (Exception exception) when (IsConnectionFailure(exception))
        {
        }
    }

    private static async Task<ClientOutcome<KeycloakToken>> ReadToken(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            return ClientOutcome<KeycloakToken>.Rejected();
        }

        if (!response.IsSuccessStatusCode)
        {
            return ClientOutcome<KeycloakToken>.Unavailable();
        }

        var token = await response.Content.ReadFromJsonAsync<KeycloakToken>(cancellationToken);
        return token is null || string.IsNullOrEmpty(token.AccessToken)
            ? ClientOutcome<KeycloakToken>.Unavailable()
            : ClientOutcome<KeycloakToken>.Success(token);
    }

    private static bool IsConnectionFailure(Exception exception) =>
        exception is HttpRequestException or TaskCanceledException or JsonException;
}
