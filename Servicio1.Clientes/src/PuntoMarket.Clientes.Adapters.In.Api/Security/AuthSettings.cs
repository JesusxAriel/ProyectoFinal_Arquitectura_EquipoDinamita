namespace PuntoMarket.Clientes.Adapters.In.Api.Security;

public sealed class AuthSettings
{
    public const string SectionName = "Authentication:Jwt";

    public string Authority { get; set; } = string.Empty;
    /// <summary>Opcional: URL de descubrimiento accesible por el servidor (DNS interno de Docker).</summary>
    public string? MetadataAddress { get; set; }
    public string Audience { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Claim que identifica al usuario para la auditoría. En Keycloak es "sub".</summary>
    public string UserIdClaim { get; set; } = "sub";

    /// <summary>Rutas (separadas por punto) de los claims donde viajan los roles del token.</summary>
    public string[] RoleClaimPaths { get; set; } = ["realm_access.roles", "roles"];

    /// <summary>Rol interno -> nombres de rol del proveedor de identidad que le corresponden.</summary>
    public Dictionary<string, string[]> RoleMapping { get; set; } = new()
    {
        [Roles.Administrador] = [Roles.Administrador],
        [Roles.Cajero] = [Roles.Cajero]
    };
}
