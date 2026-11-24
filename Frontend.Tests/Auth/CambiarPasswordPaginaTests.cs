using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Frontend;
using Xunit;

namespace PuntoMarket.Frontend.Tests.Auth;

public sealed class CambiarPasswordPaginaTests : IAsyncLifetime
{
    private WebApplication gateway = null!;
    private WebApplication frontend = null!;
    private Uri baseFrontend = null!;
    private bool debeCambiar;
    private HttpStatusCode estadoCambio = HttpStatusCode.NoContent;
    private string cuerpoCambio = "";
    private readonly List<JsonElement> cambios = [];

    public async Task InitializeAsync()
    {
        gateway = WebApplication.CreateBuilder().Build();
        gateway.Urls.Add("http://127.0.0.1:0");
        gateway.MapPost("/api/auth/login", () => Results.Json(new
        {
            accessToken = "token-de-prueba",
            expiresIn = 1800,
            username = "ana",
            nombreCompleto = "Ana Prueba",
            rol = "Cajero",
            debeCambiarPassword = debeCambiar
        }));
        gateway.MapPut("/api/usuarios/perfil/password", async (HttpContext contexto) =>
        {
            cambios.Add((await contexto.Request.ReadFromJsonAsync<JsonElement>()).Clone());
            return Results.Text(cuerpoCambio, "application/json", statusCode: (int)estadoCambio);
        });
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

    private static string ExtraerToken(string html) =>
        Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;

    private async Task<HttpClient> IniciarSesion(bool obligatorio)
    {
        debeCambiar = obligatorio;
        var cliente = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true })
        {
            BaseAddress = baseFrontend
        };
        var token = ExtraerToken(await cliente.GetStringAsync("/Auth/Login"));
        var respuesta = await cliente.PostAsync("/Auth/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Usuario"] = "ana",
            ["Password"] = "Clave#Prueba1",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        return cliente;
    }

    private static string Ruta(bool obligatorio) =>
        obligatorio ? "/Auth/CambiarPasswordObligatorio" : "/Auth/CambiarPassword";

    private static async Task<string> Enviar(HttpClient cliente, bool obligatorio, string actual, string nueva, string repetir)
    {
        var token = ExtraerToken(await cliente.GetStringAsync(Ruta(obligatorio)));
        var respuesta = await cliente.PostAsync(Ruta(obligatorio), new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["PasswordActual"] = actual,
            ["PasswordNueva"] = nueva,
            ["RepetirPasswordNueva"] = repetir,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
    }

    private static string CampoInvalido(string nombre) =>
        @"<input(?=[^>]*\bname=""" + nombre + @""")[^>]*\bclass=""[^""]*\bis-invalid\b[^""]*""";

    private static string MensajeDelCampo(string nombre, string mensaje) =>
        @"<span(?=[^>]*\bdata-valmsg-for=""" + nombre + @""")(?=[^>]*\bclass=""[^""]*\binvalid-feedback\b[^""]*"")[^>]*>" + Regex.Escape(mensaje) + "</span>";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Contrasena_actual_incorrecta_se_muestra_bajo_su_campo_sin_salir_de_la_pantalla(bool obligatorio)
    {
        estadoCambio = HttpStatusCode.BadRequest;
        cuerpoCambio = """{"error":"La contraseña actual es incorrecta."}""";
        using var cliente = await IniciarSesion(obligatorio);

        var html = await Enviar(cliente, obligatorio, "Equivocada#1", "Nueva#123", "Nueva#123");

        Assert.Matches(CampoInvalido("PasswordActual"), html);
        Assert.Matches(MensajeDelCampo("PasswordActual", "La contraseña actual no es correcta."), html);
        Assert.DoesNotMatch(CampoInvalido("PasswordNueva"), html);
        Assert.DoesNotContain("<ul><li>", html);
        Assert.Contains(obligatorio ? "Cambio de contraseña obligatorio" : "Cambiar contraseña", html);
        var siguiente = await cliente.GetAsync("/Auth/CambiarPassword" + (obligatorio ? "Obligatorio" : ""));
        Assert.Equal(HttpStatusCode.OK, siguiente.StatusCode);
        Assert.Single(cambios);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cada_error_de_validacion_sale_bajo_su_campo_y_no_en_una_lista(bool obligatorio)
    {
        using var cliente = await IniciarSesion(obligatorio);

        var html = await Enviar(cliente, obligatorio, "", "corta", "");

        Assert.Matches(CampoInvalido("PasswordActual"), html);
        Assert.Matches(CampoInvalido("PasswordNueva"), html);
        Assert.Matches(CampoInvalido("RepetirPasswordNueva"), html);
        Assert.Matches(MensajeDelCampo("PasswordActual", "Ingrese su contraseña actual."), html);
        Assert.Matches(MensajeDelCampo("PasswordNueva", "La nueva contraseña debe tener al menos 8 caracteres."), html);
        Assert.DoesNotContain("<ul><li>", html);
        Assert.Empty(cambios);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Una_repeticion_distinta_marca_solo_el_campo_de_repeticion(bool obligatorio)
    {
        using var cliente = await IniciarSesion(obligatorio);

        var html = await Enviar(cliente, obligatorio, "Actual#123", "Nueva#123", "Otra#1234");

        Assert.Matches(MensajeDelCampo("RepetirPasswordNueva", "La repetición no coincide con la nueva contraseña."), html);
        Assert.DoesNotMatch(CampoInvalido("PasswordActual"), html);
        Assert.DoesNotMatch(CampoInvalido("PasswordNueva"), html);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Una_nueva_contrasena_de_exactamente_8_caracteres_validos_no_da_error_de_longitud(bool obligatorio)
    {
        estadoCambio = HttpStatusCode.NoContent;
        using var cliente = await IniciarSesion(obligatorio);
        var token = ExtraerToken(await cliente.GetStringAsync(Ruta(obligatorio)));

        var respuesta = await cliente.PostAsync(Ruta(obligatorio), new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["PasswordActual"] = "Actual#123",
            ["PasswordNueva"] = "Abcd#123",
            ["RepetirPasswordNueva"] = "Abcd#123",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("Abcd#123", Assert.Single(cambios).GetProperty("nueva").GetString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task La_pagina_carga_el_modulo_que_limpia_el_error_al_escribir(bool obligatorio)
    {
        using var cliente = await IniciarSesion(obligatorio);

        var html = await cliente.GetStringAsync(Ruta(obligatorio));

        Assert.Contains("/js/limpiar-validacion.mjs", html);
        Assert.Contains("data-valmsg-for=\"PasswordActual\"", html);
        Assert.Contains("data-valmsg-for=\"PasswordNueva\"", html);
        Assert.Contains("data-valmsg-for=\"RepetirPasswordNueva\"", html);
    }
}
