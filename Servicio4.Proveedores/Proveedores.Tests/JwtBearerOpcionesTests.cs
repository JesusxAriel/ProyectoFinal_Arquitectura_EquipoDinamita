using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Proveedores.API.Security;

namespace Proveedores.Tests;

public class JwtBearerOpcionesTests
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
}
