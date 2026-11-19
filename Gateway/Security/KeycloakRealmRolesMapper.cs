using System.Security.Claims;
using System.Text.Json;

namespace PuntoMarket.Gateway.Security;

public static class KeycloakRealmRolesMapper
{
    private const string RealmAccessClaim = "realm_access";
    private const string RolesProperty = "roles";

    public static void AddRoleClaims(ClaimsIdentity identity)
    {
        var realmAccess = identity.FindFirst(RealmAccessClaim)?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
        {
            return;
        }

        foreach (var role in ReadRoles(realmAccess))
        {
            identity.AddClaim(new Claim(identity.RoleClaimType, role));
        }
    }

    private static IEnumerable<string> ReadRoles(string realmAccessJson)
    {
        using var document = JsonDocument.Parse(realmAccessJson);
        if (!document.RootElement.TryGetProperty(RolesProperty, out var roles)
            || roles.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return roles.EnumerateArray()
            .Select(role => role.GetString())
            .OfType<string>()
            .ToList();
    }
}
