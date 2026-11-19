using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Proveedores.Application.Dtos;

namespace Proveedores.Tests;

/// <summary>API y seed reales sobre una base temporal; no usa la base personal ni reemplaza el repositorio.</summary>
public class MongoSeedApiTests
{
    private const string Emisor = "http://seed.pruebas";
    private const string Audiencia = "proveedores-seed-test";
    // Exclusiva del host de pruebas; no es una credencial ni se registra en la API de producción.
    private static readonly SymmetricSecurityKey ClavePrueba = new(
        Encoding.UTF8.GetBytes("clave-publica-solo-tests-seed-api-0123456789"));

    private sealed class Fabrica(string uri, string baseTemporal) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(l => l.ClearProviders());
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?>
            {
                ["Mongo:ConnectionString"] = uri,
                ["Mongo:DatabaseName"] = baseTemporal,
                ["Mongo:ProveedoresCollection"] = "proveedores",
                ["Mongo:InitializeOnStartup"] = "true",
                ["Mongo:SeedEnabled"] = "true",
                ["Authentication:Jwt:Authority"] = Emisor,
                ["Authentication:Jwt:Audience"] = Audiencia,
                ["Authentication:Jwt:RequireHttpsMetadata"] = "false"
            }));
            builder.ConfigureTestServices(s => s.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
            {
                o.Authority = null;
                o.MetadataAddress = string.Empty;
                o.Configuration = new OpenIdConnectConfiguration { Issuer = Emisor };
                o.TokenValidationParameters.ValidIssuer = Emisor;
                o.TokenValidationParameters.ValidAudience = Audiencia;
                o.TokenValidationParameters.IssuerSigningKey = ClavePrueba;
            }));
        }
    }

    [MongoFact]
    public async Task BaseVacia_SeedConsultablePorApi_ReinicioConservaLosSieteDocumentos()
    {
        var uri = Environment.GetEnvironmentVariable(MongoFactAttribute.Variable)!;
        var baseTemporal = $"proveedores_seed_api_test_{Guid.NewGuid():N}";
        using var mongo = new MongoClient(uri);
        var coleccion = mongo.GetDatabase(baseTemporal).GetCollection<BsonDocument>("proveedores");
        var esperados = new[] { "PIL Andina S.A.", "EMBOL S.A. (Coca-Cola)", "Sofía SHE S.A.",
            "CBN (Cervecería Boliviana Nacional)", "Unilever Bolivia", "Industrias Venado S.A. (Kris)", "Arcor Bolivia" };
        var jwt = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Emisor, Audience = Audiencia, Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(ClavePrueba, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string,object> { ["sub"] = "seed-api-test-admin",
                ["realm_access"] = new Dictionary<string,object> { ["roles"] = new[] { "Administrador" } } }
        });
        try
        {
            Assert.Equal(0, await coleccion.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            string[]? instantanea = null;
            for (var arranque = 0; arranque < 2; arranque++)
            {
                await using var fabrica = new Fabrica(uri, baseTemporal);
                using var cliente = fabrica.CreateClient();
                cliente.DefaultRequestHeaders.Authorization = new("Bearer", jwt);
                using var r = await cliente.GetAsync("/api/proveedores?estado=Activos&tamanoPagina=100");
                Assert.Equal(HttpStatusCode.OK, r.StatusCode);
                var pagina = (await r.Content.ReadFromJsonAsync<PaginaResultado<ProveedorDto>>())!;
                Assert.Equal(7, pagina.Total);
                Assert.Equal(esperados.OrderBy(n => n), pagina.Items.Select(p => p.NombreEmpresa).OrderBy(n => n));
                Assert.All(pagina.Items, p => { Assert.True(p.EstaActivo); Assert.Equal("sistema-seed",p.IdUsuarioAdmin); });
                var docs = await coleccion.Find(FilterDefinition<BsonDocument>.Empty).Sort(Builders<BsonDocument>.Sort.Ascending("_id")).ToListAsync();
                Assert.Equal(7, docs.Count);
                Assert.All(docs, d => { Assert.Equal(0,d["version"].ToInt64()); Assert.True(d.Contains("origenSeed")); });
                var actual = docs.Select(d => d.ToJson()).ToArray();
                if (instantanea is null) instantanea = actual; else Assert.Equal(instantanea, actual);
            }
        }
        finally
        {
            // Nombre generado exclusivamente por esta prueba; nunca proveedores_db.
            await mongo.DropDatabaseAsync(baseTemporal);
        }
    }
}
