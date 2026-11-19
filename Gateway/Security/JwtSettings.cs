namespace PuntoMarket.Gateway.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Authentication:Jwt";

    public string Authority { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
    public bool ValidateAudience { get; set; }
}
