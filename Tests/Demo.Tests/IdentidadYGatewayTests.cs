extern alias Identity;
extern alias Gateway;
extern alias Clientes;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Forwarder;

namespace Demo.Tests;

public class IdentidadYGatewayTests
{
    [Theory][InlineData("Administrador",true)][InlineData("Cajero",false)]
    public async Task JwtReal_DosAudiencias_YPoliticasDefinitivasClientes(string perfil,bool baja)
    {
        await using var f=new WebApplicationFactory<Identity::Program>().WithWebHostBuilder(b=> { b.UseEnvironment("Development");b.ConfigureLogging(l=>l.ClearProviders()); });
        using var c=f.CreateClient();
        using var resp=await c.PostAsJsonAsync("/token",new {perfil}); Assert.Equal(HttpStatusCode.OK,resp.StatusCode);
        var json=await resp.Content.ReadFromJsonAsync<JsonElement>();var token=json.GetProperty("access_token").GetString()!;
        var jwks=await c.GetStringAsync("/realms/proveedores-dev/protocol/openid-connect/certs");
        var h=new JsonWebTokenHandler();
        var auth=new Clientes::PuntoMarket.Clientes.Adapters.In.Api.Security.AuthSettings();
        var config=new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var servicios=new ServiceCollection();servicios.AddLogging();
        Clientes::PuntoMarket.Clientes.Adapters.In.Api.Security.AuthenticationExtensions.AddClientesAuthentication(servicios,config);
        using var provider=servicios.BuildServiceProvider();
        foreach(var audience in new[]{"clientes-api","proveedores-api","servicio-no-autorizado"})
        {
            var parameters=new TokenValidationParameters { ValidateIssuer=true,ValidIssuer="http://localhost:5300/realms/proveedores-dev",ValidateAudience=true,ValidAudience=audience,ValidateLifetime=true,ValidateIssuerSigningKey=true,IssuerSigningKeys=new JsonWebKeySet(jwks).GetSigningKeys(),ValidAlgorithms=[SecurityAlgorithms.RsaSha256],RoleClaimType=ClaimTypes.Role };
            var result=await h.ValidateTokenAsync(token,parameters);
            Assert.Equal(audience!="servicio-no-autorizado",result.IsValid);
            if(audience!="clientes-api")continue;
            Clientes::PuntoMarket.Clientes.Adapters.In.Api.Security.KeycloakRolesClaimsMapper.Aplicar(result.ClaimsIdentity,auth);
            var principal=new ClaimsPrincipal(result.ClaimsIdentity);var authorization=provider.GetRequiredService<IAuthorizationService>();
            Assert.True((await authorization.AuthorizeAsync(principal,null,"ClientesLectura")).Succeeded);
            Assert.True((await authorization.AuthorizeAsync(principal,null,"ClientesGestion")).Succeeded);
            Assert.Equal(baja,(await authorization.AuthorizeAsync(principal,null,"ClientesBaja")).Succeeded);
            parameters.ValidIssuer="http://otro-emisor";Assert.False((await h.ValidateTokenAsync(token,parameters)).IsValid);
        }
        var keys=JsonDocument.Parse(jwks);Assert.False(keys.RootElement.GetProperty("keys")[0].TryGetProperty("d",out _));
    }
    [Fact] public async Task Identidad_NoAceptaRolesInventados()
    {
        await using var f=new WebApplicationFactory<Identity::Program>().WithWebHostBuilder(b=>b.UseEnvironment("Development"));using var c=f.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/token",new {perfil="SuperAdministrador"})).StatusCode);
    }
    [Fact] public void ConfiguracionClientes_MetadataInternaNoDebilitaJwt()
    {
        var options=new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions();
        new Clientes::PuntoMarket.Clientes.Adapters.In.Api.Security.ConfigureJwtBearerOptions(
            Microsoft.Extensions.Options.Options.Create(new Clientes::PuntoMarket.Clientes.Adapters.In.Api.Security.AuthSettings
            { Authority="http://localhost:15300/realms/proveedores-dev",MetadataAddress="http://identity-dev:8080/realms/proveedores-dev/.well-known/openid-configuration",Audience="clientes-api",RequireHttpsMetadata=false }))
            .Configure(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,options);
        Assert.Equal("clientes-api",options.Audience);Assert.Contains("identity-dev:8080",options.MetadataAddress);
        Assert.True(options.TokenValidationParameters.ValidateIssuer);Assert.True(options.TokenValidationParameters.ValidateAudience);
        Assert.True(options.TokenValidationParameters.ValidateLifetime);Assert.True(options.TokenValidationParameters.ValidateIssuerSigningKey);
    }
    private sealed class Destinos : IForwarderHttpClientFactory
    {
        public List<(string Metodo,string Url,string? Bearer)> Recibidas { get; }=[];
        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)=>new(new Handler(this));
        private sealed class Handler(Destinos destinos):HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct)
            {
                destinos.Recibidas.Add((r.Method.Method,r.RequestUri!.ToString(),r.Headers.Authorization?.ToString()));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new {ok=true})});
            }
        }
    }
    [Theory][InlineData("GET","/api/clientes","5400")][InlineData("PUT","/api/clientes/2","5400")][InlineData("PATCH","/api/clientes/2/reactivar","5400")][InlineData("DELETE","/api/clientes/2","5400")][InlineData("GET","/api/proveedores","5200")][InlineData("POST","/dev-auth/token","5300")]
    public async Task GatewayDemo_ReenviaRutaMetodoYBearer(string method,string path,string port)
    {
        var destinos=new Destinos();await using var f=new WebApplicationFactory<Gateway::Program>().WithWebHostBuilder(b=>
        {b.UseEnvironment("Development");b.ConfigureLogging(l=>l.ClearProviders());b.ConfigureTestServices(s=> {s.RemoveAll<IForwarderHttpClientFactory>();s.AddSingleton<IForwarderHttpClientFactory>(destinos);});});
        using var c=f.CreateClient();using var req=new HttpRequestMessage(new HttpMethod(method),path);req.Headers.TryAddWithoutValidation("Authorization","Bearer jwt-test");
        var r=await c.SendAsync(req);Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        var item=Assert.Single(destinos.Recibidas);Assert.Equal(method,item.Metodo);Assert.Contains(":"+port+"/",item.Url);Assert.Equal("Bearer jwt-test",item.Bearer);
        Assert.EndsWith(path.StartsWith("/dev-auth") ? "/token" : path,item.Url);
    }
}
