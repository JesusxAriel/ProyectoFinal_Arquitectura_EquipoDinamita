namespace PuntoMarket.Usuarios.Infrastructure.Identity;

public sealed class KeycloakOpciones
{
    public const string Seccion = "Keycloak";

    public string BaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string AdminRealm { get; set; } = "master";
    public string AdminClientId { get; set; } = "admin-cli";
    public string AdminUserName { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string LoginClientId { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 10;

    public Uri UrlToken(string realm) => new($"{BaseUrlNormalizada()}realms/{realm}/protocol/openid-connect/token");

    public Uri UrlAdmin(string ruta) => new($"{BaseUrlNormalizada()}admin/realms/{Realm}/{ruta}");

    public void Validar()
    {
        var faltantes = new[]
        {
            (nameof(BaseUrl), BaseUrl), (nameof(Realm), Realm), (nameof(AdminUserName), AdminUserName),
            (nameof(AdminPassword), AdminPassword), (nameof(LoginClientId), LoginClientId)
        }.Where(campo => string.IsNullOrWhiteSpace(campo.Item2)).Select(campo => $"{Seccion}:{campo.Item1}").ToList();

        if (faltantes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Falta la configuración de Keycloak: {string.Join(", ", faltantes)}.");
        }
    }

    private string BaseUrlNormalizada() => BaseUrl.TrimEnd('/') + "/";
}
