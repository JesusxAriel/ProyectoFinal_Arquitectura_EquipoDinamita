using System.Security.Claims;
using Proveedores.API.Security;

namespace Proveedores.Tests;

public class KeycloakRolesClaimsMapperTests
{
    private static AuthSettings Settings() => new()
    {
        RoleClaimPaths = ["realm_access.roles", "resource_access.proveedores-api.roles", "roles"],
        RoleMapping = new()
        {
            [Roles.Administrador] = ["admin"],
            [Roles.Cajero] = ["cajero", "cashier"]
        }
    };

    private static ClaimsIdentity Identidad(params Claim[] claims) => new(claims, "test");

    [Fact]
    public void RealmAccess_MapeaRolExternoARolInterno()
    {
        var id = Identidad(new Claim("realm_access", "{\"roles\":[\"admin\",\"offline_access\"]}"));
        KeycloakRolesClaimsMapper.Aplicar(id, Settings());

        Assert.True(id.HasClaim(ClaimTypes.Role, Roles.Administrador));
        Assert.False(id.HasClaim(ClaimTypes.Role, Roles.Cajero));
    }

    [Fact]
    public void ResourceAccess_PorCliente_Funciona()
    {
        var id = Identidad(new Claim("resource_access", "{\"proveedores-api\":{\"roles\":[\"cashier\"]}}"));
        KeycloakRolesClaimsMapper.Aplicar(id, Settings());
        Assert.True(id.HasClaim(ClaimTypes.Role, Roles.Cajero));
    }

    [Fact]
    public void ClaimPlanoRoles_VariosClaimsOArregloJson()
    {
        var a = Identidad(new Claim("roles", "Administrador"));
        KeycloakRolesClaimsMapper.Aplicar(a, Settings());
        Assert.True(a.HasClaim(ClaimTypes.Role, Roles.Administrador));

        var b = Identidad(new Claim("roles", "[\"cajero\"]"));
        KeycloakRolesClaimsMapper.Aplicar(b, Settings());
        Assert.True(b.HasClaim(ClaimTypes.Role, Roles.Cajero));
    }

    [Fact]
    public void SinRolesConocidos_NoAgregaNada_YClaimMalformadoSeIgnora()
    {
        var id = Identidad(new Claim("realm_access", "no-es-json"), new Claim("sub", "x"));
        KeycloakRolesClaimsMapper.Aplicar(id, Settings());
        Assert.DoesNotContain(id.Claims, c => c.Type == ClaimTypes.Role);
    }

    [Fact]
    public void ComparacionNoDistingueMayusculas()
    {
        var id = Identidad(new Claim("realm_access", "{\"roles\":[\"ADMIN\"]}"));
        KeycloakRolesClaimsMapper.Aplicar(id, Settings());
        Assert.True(id.HasClaim(ClaimTypes.Role, Roles.Administrador));
    }
}
