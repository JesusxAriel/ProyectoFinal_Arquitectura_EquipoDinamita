extern alias Identity;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Proveedores.API.Security;

namespace Proveedores.Tests;

public class DockerIdentityTests
{
    private const string Emisor = "http://localhost:5300/realms/proveedores-dev";

    [Theory]
    [InlineData(null)]
    [InlineData("http://identity-dev:8080/realms/proveedores-dev/.well-known/openid-configuration")]
    public void MetadataInterna_NoDesactivaLasValidacionesNiCambiaEmisor(string? metadata)
    {
        var opciones = new JwtBearerOptions();
        new ConfigureJwtBearerOptions(Options.Create(new AuthSettings { Authority = Emisor,
            MetadataAddress = metadata, Audience = "proveedores-api", RequireHttpsMetadata = false }))
            .Configure(JwtBearerDefaults.AuthenticationScheme, opciones);
        Assert.Equal(Emisor, opciones.Authority);
        if (metadata is not null) Assert.Equal(metadata, opciones.MetadataAddress);
        Assert.True(opciones.TokenValidationParameters.ValidateIssuer);
        Assert.True(opciones.TokenValidationParameters.ValidateAudience);
        Assert.True(opciones.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.True(opciones.TokenValidationParameters.ValidateLifetime);
    }

    [Theory]
    [InlineData("http://localhost:5300")]
    [InlineData("http://identity-dev:8080")]
    public async Task Descubrimiento_ConservaIssuerPublico_JwksAccesibleDesdeOrigen_YFirmaRs256(string origen)
    {
        await using var fabrica = new WebApplicationFactory<Identity::Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string,string?>
            { ["DevIdentity:Issuer"] = Emisor, ["DevIdentity:Audience"] = "proveedores-api" }));
        });
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri(origen) });
        var descubrimiento = (await cliente.GetFromJsonAsync<JsonElement>("/realms/proveedores-dev/.well-known/openid-configuration"));
        Assert.Equal(Emisor, descubrimiento.GetProperty("issuer").GetString());
        var jwksUri = descubrimiento.GetProperty("jwks_uri").GetString()!;
        Assert.Equal(origen + "/realms/proveedores-dev/protocol/openid-connect/certs", jwksUri);
        var jwksTexto = await cliente.GetStringAsync(jwksUri);
        using var jwks = JsonDocument.Parse(jwksTexto);
        Assert.False(jwks.RootElement.GetProperty("keys")[0].TryGetProperty("d", out _));
        using var respuesta = await cliente.PostAsJsonAsync("/token", new { perfil = "Administrador" });
        var datos = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var token = datos.GetProperty("access_token").GetString()!;
        var handler = new JsonWebTokenHandler();
        Assert.Equal("RS256", handler.ReadJsonWebToken(token).Alg);
        var validacion = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = Emisor,
            ValidateAudience = true, ValidAudience = "proveedores-api",
            ValidateIssuerSigningKey = true, IssuerSigningKeys = new JsonWebKeySet(jwksTexto).GetSigningKeys(),
            ValidateLifetime = true, ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        });
        Assert.True(validacion.IsValid);
    }
}
