using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Productos.Adapters.In.Api.Integrations;
using PuntoMarket.Productos.Application.Ports.Out;
using Npgsql;
namespace PuntoMarket.Productos.Tests;

public class HttpYAutorizacionTests
{
    [Theory] [InlineData(404,"Producto.ProveedorNoEncontrado")] [InlineData(503,"Proveedor.NoDisponible")]
    [InlineData(401,"Proveedor.NoDisponible")] [InlineData(403,"Proveedor.NoDisponible")]
    public async Task ProveedorDistingueErrores(int status,string code) {
        var adapter=Adapter(status,"{}"); var r=await adapter.ValidarActivoAsync("p1",default);
        Assert.Equal(code,r.Error.Code);
    }
    [Theory] [InlineData("{\"id\":\"p1\",\"estaActivo\":true}",true)]
    [InlineData("{\"id\":\"p1\",\"estaActivo\":false}",false)] [InlineData("{}",false)]
    [InlineData("{\"id\":\"p1\"}",false)] [InlineData("{\"id\":\"otro\",\"estaActivo\":true}",false)]
    public async Task ProveedorNoAsumeActivo(string body,bool success) {
        Assert.Equal(success,(await Adapter(200,body).ValidarActivoAsync("p1",default)).IsSuccess);
    }
    private static ProveedorApiAdapter Adapter(int status,string body) {
        var context=new DefaultHttpContext(); context.Request.Headers.Authorization="Bearer token-prueba";
        return new(new HttpClient(new Handler(status,body)){BaseAddress=new Uri("http://proveedores.test/")},new HttpContextAccessor{HttpContext=context});
    }
    private sealed class Handler(int status,string body):HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req,CancellationToken ct) {
            Assert.Equal("Bearer token-prueba",req.Headers.Authorization?.ToString());
            Assert.Equal("/api/proveedores/p1",req.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status){Content=new StringContent(body,System.Text.Encoding.UTF8,"application/json")});
        }
    }
    [Fact] public async Task SinJwtRechazaYSaludEsAnonima() {
        await using var host=new ApiFactory(); using var client=host.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/productos")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/salud")).StatusCode);
    }
    [Theory] [InlineData("Cajero",403)] [InlineData("Administrador",201)]
    public async Task JwtRealAplicaPermisos(string rol,int expected) {
        await using var host=new ApiFactory(); using var client=host.CreateClient();
        client.DefaultRequestHeaders.Authorization=new("Bearer",host.Token(rol));
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/productos")).StatusCode);
        Assert.Equal((HttpStatusCode)expected,(await client.PostAsJsonAsync("/api/productos",ReglasYCasesTests.Datos)).StatusCode);
        Assert.Equal((HttpStatusCode)(rol=="Cajero"?403:200),(await client.PutAsJsonAsync("/api/productos/1",ReglasYCasesTests.Datos)).StatusCode);
        Assert.Equal((HttpStatusCode)(rol=="Cajero"?403:204),(await client.DeleteAsync("/api/productos/1")).StatusCode);
    }
    [Theory] [InlineData("otra-audiencia","https://identity.test")] [InlineData("productos-api","https://otro.test")]
    public async Task JwtConIssuerOAudienciaIncorrectosEsRechazado(string audience,string issuer) {
        await using var host=new ApiFactory(); using var client=host.CreateClient();
        client.DefaultRequestHeaders.Authorization=new("Bearer",host.Token("Administrador",audience,issuer));
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/productos")).StatusCode);
    }
    [Fact] public async Task DataSourceTieneLifetimeSingleton() {
        await using var host=new ApiFactory(); using var client=host.CreateClient();
        using var a=host.Services.CreateScope(); using var b=host.Services.CreateScope();
        Assert.Same(a.ServiceProvider.GetRequiredService<NpgsqlDataSource>(),b.ServiceProvider.GetRequiredService<NpgsqlDataSource>());
    }
    private sealed class ApiFactory:WebApplicationFactory<Program> {
        private readonly SymmetricSecurityKey key=new(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            builder.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{
                ["Productos:InitializeOnStartup"]="false",["ConnectionStrings:PostgresConnection"]="Host=localhost;Database=no_conectar;Username=test",
                ["Authentication:Jwt:Authority"]="https://identity.test",["Authentication:Jwt:Audience"]="productos-api"
            }));
            builder.ConfigureServices(s=>{
                s.AddScoped<IProductoRepository,ReglasYCasesTests.Repo>();
                s.AddScoped<IProveedorApiClient,ReglasYCasesTests.Proveedor>();
                s.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,o=>{
                    o.Configuration=new OpenIdConnectConfiguration{Issuer="https://identity.test"};
                    o.Configuration.SigningKeys.Add(key);
                    o.ConfigurationManager=new Microsoft.IdentityModel.Protocols.StaticConfigurationManager<OpenIdConnectConfiguration>(o.Configuration);
                    o.TokenValidationParameters.ValidIssuer="https://identity.test";
                });
            });
        }
        public string Token(string role,string audience="productos-api",string issuer="https://identity.test") => new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(issuer,audience,[new Claim("sub","sub-de-prueba"),new Claim("realm_access","{\"roles\":[\""+role+"\"]}")],
                expires:DateTime.UtcNow.AddMinutes(5),signingCredentials:new SigningCredentials(key,SecurityAlgorithms.HmacSha256)));
    }
}
