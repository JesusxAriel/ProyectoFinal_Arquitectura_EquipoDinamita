using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PuntoMarket.Usuarios.Api.Security;
using PuntoMarket.Usuarios.Infrastructure.Email;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Seguridad;

public sealed class SeguridadTests
{
    private static ClaimsIdentity IdentidadConRealmAccess(string? realmAccess)
    {
        var identidad = new ClaimsIdentity("Bearer");
        if (realmAccess is not null)
        {
            identidad.AddClaim(new Claim("realm_access", realmAccess));
        }

        return identidad;
    }

    private static List<string> Roles(ClaimsIdentity identidad) =>
        identidad.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList();

    [Fact]
    public void Mapper_RealmAccessConRoles_AgregaUnClaimDeRolPorCadaUno()
    {
        var identidad = IdentidadConRealmAccess("""{"roles":["Administrador","Cajero"]}""");

        KeycloakRolesMapper.AgregarRoles(identidad);

        Assert.Equal(["Administrador", "Cajero"], Roles(identidad));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("""{"otro":[]}""")]
    [InlineData("""{"roles":"Administrador"}""")]
    [InlineData("no es json")]
    public void Mapper_ClaimAusenteOInvalido_NoAgregaRoles(string? realmAccess)
    {
        var identidad = IdentidadConRealmAccess(realmAccess);

        KeycloakRolesMapper.AgregarRoles(identidad);

        Assert.Empty(Roles(identidad));
    }

    [Fact]
    public void Mapper_LosRolesAgregadosSonReconocidosPorIsInRole()
    {
        var identidad = IdentidadConRealmAccess("""{"roles":["Administrador"]}""");

        KeycloakRolesMapper.AgregarRoles(identidad);

        Assert.True(new ClaimsPrincipal(identidad).IsInRole("Administrador"));
    }

    private static UsuarioActualHttp UsuarioActualCon(params Claim[] claims)
    {
        var contexto = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"))
        };
        return new UsuarioActualHttp(new HttpContextAccessor { HttpContext = contexto });
    }

    [Fact]
    public void UsuarioActual_LeeElUsernameDelToken()
    {
        var usuario = UsuarioActualCon(
            new Claim("preferred_username", "admin.test"), new Claim(ClaimTypes.Role, "Administrador"));

        Assert.Equal("admin.test", usuario.Username);
    }

    [Fact]
    public void UsuarioActual_SinClaimDeUsername_LanzaExcepcionTecnica()
    {
        var usuario = UsuarioActualCon(new Claim(ClaimTypes.Role, "Cajero"));

        Assert.Throws<InvalidOperationException>(() => usuario.Username);
    }

    [Fact]
    public void CorreoDeActivacion_IncluyeUsernameYEnlaceSinContrasena()
    {
        var enlace = "http://localhost:5100/Auth/Activar?token=abc";

        var cuerpo = SmtpEnviadorCorreo.ComponerCuerpoActivacion("jperez", enlace, TimeSpan.FromMinutes(60));

        Assert.Contains("Nombre de usuario: jperez", cuerpo);
        Assert.Contains(enlace, cuerpo);
        Assert.Contains("vence en 1 hora", cuerpo);
        Assert.Contains("una sola vez", cuerpo);
        Assert.DoesNotContain("ontraseña temporal", cuerpo);
        Assert.DoesNotContain("<", cuerpo);
    }
}
