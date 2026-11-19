namespace PuntoMarket.Usuarios.Api.Security;

public sealed class JwtOpciones
{
    public const string Seccion = "Authentication:Jwt";

    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
}
