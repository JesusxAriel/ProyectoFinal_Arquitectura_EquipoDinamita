namespace PuntoMarket.Gateway.Auth;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string TokenEndpoint { get; set; } = string.Empty;
    public string LogoutEndpoint { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 10;
}

public sealed class UsuariosOptions
{
    public const string SectionName = "Usuarios";

    public string BaseUrl { get; set; } = string.Empty;
    public string CurrentUserPath { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 10;
}
