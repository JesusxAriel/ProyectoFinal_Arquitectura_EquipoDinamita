using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Proveedores.Application.Abstractions;

namespace Proveedores.Tests;

/// <summary>
/// Levanta la API real en memoria (pipeline, JwtBearer, políticas y controladores reales) pero:
///  - reemplaza MongoDB por un repositorio en memoria (no requiere Docker),
///  - valida tokens firmados con una clave simétrica de PRUEBAS (en producción se usa el JWKS de Keycloak).
/// Los tokens de prueba incluyen los roles en la forma de Keycloak (realm_access.roles).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "http://issuer.pruebas";
    public const string Audience = "proveedores-api";
    private static readonly SymmetricSecurityKey Clave =
        new(Encoding.UTF8.GetBytes("clave-solo-para-pruebas-automatizadas-0123456789"));

    public InMemoryProveedorRepository Repositorio { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mongo:InitializeOnStartup"] = "false",
            ["Authentication:Jwt:Authority"] = Issuer,
            ["Authentication:Jwt:Audience"] = Audience,
            ["Authentication:Jwt:RequireHttpsMetadata"] = "false"
        }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IProveedorRepository>();
            services.AddSingleton<IProveedorRepository>(Repositorio);

            // Se ejecuta en la fase "Configure" (antes de los PostConfigure de JwtBearer): evita descubrir metadatos por red.
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
            {
                o.Authority = null;
                o.MetadataAddress = string.Empty;
                o.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                o.TokenValidationParameters.ValidIssuer = Issuer;
                o.TokenValidationParameters.ValidAudience = Audience;
                o.TokenValidationParameters.IssuerSigningKey = Clave;
            });
        });
    }

    /// <summary>Genera un JWT de pruebas con el claim sub y los roles en formato Keycloak.</summary>
    public static string Token(string sub, string? rol, TimeSpan? vigencia = null, string audience = Audience)
    {
        var claims = new Dictionary<string, object> { ["sub"] = sub };
        if (rol is not null)
            claims["realm_access"] = new Dictionary<string, object> { ["roles"] = new[] { rol } };

        var expira = DateTime.UtcNow.Add(vigencia ?? TimeSpan.FromMinutes(30));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Claims = claims,
            NotBefore = expira.AddMinutes(-30), // siempre anterior a Expires (también para tokens ya expirados)
            Expires = expira,
            SigningCredentials = new SigningCredentials(Clave, SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public HttpClient ClienteCon(string? token)
    {
        var cliente = CreateClient();
        if (token is not null)
            cliente.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}
