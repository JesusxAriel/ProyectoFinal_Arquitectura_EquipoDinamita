using System.Text.Json.Serialization;

namespace PuntoMarket.Gateway.Auth;

public sealed record LoginRequest(string? Username, string? Password);

public sealed record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    string Username,
    string NombreCompleto,
    string Rol,
    bool DebeCambiarPassword);

public sealed record LogoutRequest(string? RefreshToken);

public sealed record UsuarioActual(
    string Username,
    string NombreCompleto,
    string Rol,
    bool Activo,
    bool DebeCambiarPassword);

public sealed record KeycloakToken(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);
