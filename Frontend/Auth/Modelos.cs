namespace PuntoMarket.Frontend.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    string Username,
    string NombreCompleto,
    string Rol,
    bool DebeCambiarPassword);

public sealed record DatosSesion(
    string Username,
    string NombreCompleto,
    string Rol,
    bool DebeCambiarPassword,
    DateTimeOffset ExpiraEn);

public sealed record ErrorApi(string? Error);
