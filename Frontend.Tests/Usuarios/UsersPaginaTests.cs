using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PuntoMarket.Frontend;
using Xunit;

namespace PuntoMarket.Frontend.Tests.Usuarios;

public sealed class UsersPaginaTests : IAsyncLifetime
{
    private WebApplication gateway = null!;
    private WebApplication frontend = null!;
    private Uri baseFrontend = null!;

    public async Task InitializeAsync()
    {
        var builderGateway = WebApplication.CreateBuilder();
        gateway = builderGateway.Build();
        gateway.Urls.Add("http://127.0.0.1:0");
        gateway.MapPost("/api/auth/login", async (HttpContext contexto) =>
        {
            var cuerpo = await contexto.Request.ReadFromJsonAsync<Dictionary<string, string>>();
            var usuario = cuerpo!["username"];
            var rol = usuario == "admin" ? "Administrador" : "Cajero";
            return Results.Json(new
            {
                accessToken = "token-de-prueba",
                expiresIn = 1800,
                username = usuario,
                nombreCompleto = "Usuario de prueba",
                rol,
                debeCambiarPassword = false
            });
        });
        gateway.MapGet("/api/usuarios", () => Results.Json(new[]
        {
            new { id = 1, username = "admin", nombreCompleto = "Usuario de prueba", correo = "admin@correo.test", rol = "Administrador", activo = true, debeCambiarPassword = false }
        }));
        gateway.MapGet("/api/usuarios/roles", () => Results.Json(new[] { new { id = 1, nombre = "Administrador" }, new { id = 2, nombre = "Cajero" } }));
        await gateway.StartAsync();

        frontend = FrontendBootstrap.Crear([$"--GatewayApi:BaseUrl={Direccion(gateway)}/", "--applicationName=PuntoMarket.Frontend"]);
        frontend.Urls.Add("http://127.0.0.1:0");
        await frontend.StartAsync();
        baseFrontend = new Uri(Direccion(frontend));
    }

    public async Task DisposeAsync()
    {
        await frontend.DisposeAsync();
        await gateway.DisposeAsync();
    }

    private static string Direccion(WebApplication aplicacion) =>
        aplicacion.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

    private async Task<HttpClient> IniciarSesion(string usuario)
    {
        var cliente = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true })
        {
            BaseAddress = baseFrontend
        };
        var pagina = await cliente.GetStringAsync("/Auth/Login");
        var token = Regex.Match(pagina, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        var respuesta = await cliente.PostAsync("/Auth/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Usuario"] = usuario,
            ["Password"] = "Clave#Prueba1",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        return cliente;
    }

    [Fact]
    public async Task El_administrador_ve_el_listado_de_usuarios()
    {
        using var cliente = await IniciarSesion("admin");

        var respuesta = await cliente.GetAsync("/Users");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("admin@correo.test", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task El_cajero_es_redirigido_a_inicio_y_no_ve_el_listado()
    {
        using var cliente = await IniciarSesion("cajero");

        var respuesta = await cliente.GetAsync("/Users");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Index", respuesta.Headers.Location!.OriginalString);
        Assert.DoesNotContain("admin@correo.test", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Sin_sesion_se_redirige_al_login()
    {
        using var cliente = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = baseFrontend };

        var respuesta = await cliente.GetAsync("/Users");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Auth/Login", respuesta.Headers.Location!.OriginalString);
    }
}