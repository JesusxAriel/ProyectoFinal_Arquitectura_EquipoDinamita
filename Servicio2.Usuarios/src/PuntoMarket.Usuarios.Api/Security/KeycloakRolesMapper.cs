using System.Security.Claims;
using System.Text.Json;

namespace PuntoMarket.Usuarios.Api.Security;

public static class KeycloakRolesMapper
{
    private const string RealmAccessClaim = "realm_access";
    private const string RolesProperty = "roles";

    public static void AgregarRoles(ClaimsIdentity identidad)
    {
        var realmAccess = identidad.FindFirst(RealmAccessClaim)?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
        {
            return;
        }

        foreach (var rol in LeerRoles(realmAccess))
        {
            identidad.AddClaim(new Claim(identidad.RoleClaimType, rol));
        }
    }

    private static IReadOnlyList<string> LeerRoles(string realmAccessJson)
    {
        try
        {
            using var documento = JsonDocument.Parse(realmAccessJson);
            if (!documento.RootElement.TryGetProperty(RolesProperty, out var roles)
                || roles.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return roles.EnumerateArray()
                .Select(rol => rol.GetString())
                .OfType<string>()
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
