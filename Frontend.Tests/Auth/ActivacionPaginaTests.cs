using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PuntoMarket.Frontend.Auth;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ActivarCuenta;

namespace PuntoMarket.Frontend.Tests.Auth;

public sealed class ActivacionPaginaTests : IAsyncLifetime
{
    private const string Token = "tok-secreto-1234";
    private const string PasswordNueva = "NuevaClave#2026";
    private const string Ruta = "/Auth/Activar";

    private sealed record Llamada(string Metodo, string Ruta, bool ConAutorizacion, string Cuerpo);

    private sealed class Navegador
    {
        public Dictionary<string, string> Cookies { get; } = [];
    }

    private readonly List<Llamada> llamadas = [];
    private WebApplication gateway = null!;
    private WebApplication frontend = null!;
    private HttpClient cliente = null!;

    private int estadoDelGateway = 200;
    private string cuerpoDelGateway = """{"activada":true}""";

    public async Task InitializeAsync()
    {
        var constructor = WebApplication.CreateBuilder();
        gateway = constructor.Build();
        gateway.Urls.Add("http://127.0.0.1:0");
        gateway.MapPost("/api/activacion", async (HttpContext contexto) =>
        {
            using var lector = new StreamReader(contexto.Request.Body);
            var cuerpo = await lector.ReadToEndAsync();
            lock (llamadas)
                llamadas.Add(new Llamada(
                    contexto.Request.Method,
                    contexto.Request.Path,
                    contexto.Request.Headers.ContainsKey("Authorization"),
                    cuerpo));
            return Results.Content(cuerpoDelGateway, "application/json", statusCode: estadoDelGateway);
        });
        await gateway.StartAsync();

        frontend = FrontendBootstrap.Crear(
            [$"--GatewayApi:BaseUrl={Direccion(gateway)}/", "--applicationName=PuntoMarket.Frontend"]);
        frontend.Urls.Add("http://127.0.0.1:0");
        await frontend.StartAsync();

        cliente = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
        {
            BaseAddress = new Uri(Direccion(frontend))
        };
    }

    public async Task DisposeAsync()
    {
        cliente.Dispose();
        await frontend.DisposeAsync();
        await gateway.DisposeAsync();
    }

    private static string Direccion(WebApplication aplicacion) =>
        aplicacion.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

    private async Task<HttpResponseMessage> Enviar(Navegador navegador, HttpRequestMessage peticion)
    {
        if (navegador.Cookies.Count > 0)
            peticion.Headers.Add("Cookie", string.Join("; ", navegador.Cookies.Select(c => $"{c.Key}={c.Value}")));

        var respuesta = await cliente.SendAsync(peticion);

        foreach (var linea in respuesta.Headers.TryGetValues("Set-Cookie", out var valores) ? valores : [])
        {
            var par = linea.Split(';')[0];
            var separador = par.IndexOf('=');
            var nombre = par[..separador];
            var valor = par[(separador + 1)..];
            var vencida = linea.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(valor) || vencida)
                navegador.Cookies.Remove(nombre);
            else
                navegador.Cookies[nombre] = valor;
        }

        return respuesta;
    }

    private Task<HttpResponseMessage> Obtener(Navegador navegador, string ruta) =>
        Enviar(navegador, new HttpRequestMessage(HttpMethod.Get, ruta));

    private Task<HttpResponseMessage> Publicar(Navegador navegador, string ruta, Dictionary<string, string> campos) =>
        Enviar(navegador, new HttpRequestMessage(HttpMethod.Post, ruta) { Content = new FormUrlEncodedContent(campos) });

    private async Task<Navegador> AbrirEnlace(string token = Token)
    {
        var navegador = new Navegador();
        var respuesta = await Obtener(navegador, $"{Ruta}?token={Uri.EscapeDataString(token)}");
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        return navegador;
    }

    private static async Task<string> Antiforgery(HttpResponseMessage formulario)
    {
        var html = await formulario.Content.ReadAsStringAsync();
        var coincidencia = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(coincidencia.Success);
        return coincidencia.Groups[1].Value;
    }

    private async Task<HttpResponseMessage> EnviarFormulario(
        Navegador navegador, string nueva = PasswordNueva, string confirmacion = PasswordNueva)
    {
        var formulario = await Obtener(navegador, Ruta);
        Assert.Equal(HttpStatusCode.OK, formulario.StatusCode);
        return await Publicar(navegador, Ruta, new Dictionary<string, string>
        {
            ["NuevaContrasena"] = nueva,
            ["ConfirmarContrasena"] = confirmacion,
            ["__RequestVerificationToken"] = await Antiforgery(formulario)
        });
    }

    private static async Task<string> Texto(HttpResponseMessage respuesta) =>
        WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

    private static void VerificarCabecerasPrivadas(HttpResponseMessage respuesta)
    {
        Assert.Contains("no-store", string.Join(",", respuesta.Headers.GetValues("Cache-Control")));
        Assert.Equal("no-referrer", Assert.Single(respuesta.Headers.GetValues("Referrer-Policy")));
    }

    [Fact]
    public async Task El_enlace_con_token_guarda_la_referencia_y_redirige_sin_consulta()
    {
        var navegador = new Navegador();

        var respuesta = await Obtener(navegador, $"{Ruta}?token={Token}");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(Ruta, respuesta.Headers.Location!.OriginalString);
        var cookie = Assert.Single(respuesta.Headers.GetValues("Set-Cookie"), c => c.StartsWith(ActivarCookie + "="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/Auth/Activar", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=3600", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Matches("^[0-9A-F]{64}$", navegador.Cookies[ActivarCookie]);
        Assert.DoesNotContain(Token, cookie);
        VerificarCabecerasPrivadas(respuesta);
        Assert.Empty(llamadas);
    }

    private const string ActivarCookie = "PuntoMarket.Activacion";

    [Fact]
    public async Task El_formulario_no_expone_el_token_y_no_trae_valores_de_contrasena()
    {
        var navegador = await AbrirEnlace();

        var respuesta = await Obtener(navegador, Ruta);
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.DoesNotContain(Token, html);
        Assert.Contains("name=\"__RequestVerificationToken\"", html);
        Assert.Equal(2, Regex.Matches(html, "type=\"password\"").Count);
        Assert.Equal(2, Regex.Matches(html, "autocomplete=\"new-password\"").Count);
        Assert.DoesNotMatch("type=\"password\"[^>]*value=", html);
        VerificarCabecerasPrivadas(respuesta);
    }

    [Fact]
    public async Task Activa_de_forma_anonima_sin_JWT_y_redirige_al_login_con_el_mensaje()
    {
        var navegador = await AbrirEnlace();

        var respuesta = await EnviarFormulario(navegador);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Auth/Login", respuesta.Headers.Location!.OriginalString);
        VerificarCabecerasPrivadas(respuesta);
        var llamada = Assert.Single(llamadas);
        Assert.Equal("POST", llamada.Metodo);
        Assert.Equal("/api/activacion", llamada.Ruta);
        Assert.False(llamada.ConAutorizacion);
        Assert.Contains($"\"token\":\"{Token}\"", llamada.Cuerpo);
        Assert.Contains($"\"nuevaContrasena\":\"{PasswordNueva}\"", llamada.Cuerpo);
        Assert.Contains($"\"confirmarContrasena\":\"{PasswordNueva}\"", llamada.Cuerpo);
        Assert.False(navegador.Cookies.ContainsKey(ActivarCookie));

        var login = await Obtener(navegador, "/Auth/Login");
        var html = await Texto(login);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains("alert-success", html);
        Assert.Contains(MensajesActivacion.ActivacionExitosa, html);
    }

    [Fact]
    public async Task El_mensaje_de_exito_se_muestra_una_sola_vez()
    {
        var navegador = await AbrirEnlace();
        await EnviarFormulario(navegador);
        await Obtener(navegador, "/Auth/Login");

        var segundo = await Texto(await Obtener(navegador, "/Auth/Login"));

        Assert.DoesNotContain(MensajesActivacion.ActivacionExitosa, segundo);
    }

    [Fact]
    public async Task Tras_activar_el_mismo_enlace_ya_no_tiene_referencia_en_el_servidor()
    {
        var navegador = await AbrirEnlace();
        var referencia = navegador.Cookies[ActivarCookie];
        await EnviarFormulario(navegador);

        var respuesta = await Obtener(new Navegador { Cookies = { [ActivarCookie] = referencia } }, Ruta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(MensajesActivacion.ReferenciaExpirada, await Texto(respuesta));
    }

    [Fact]
    public async Task Un_POST_sin_antiforgery_se_rechaza_sin_llamar_al_gateway()
    {
        var navegador = await AbrirEnlace();

        var respuesta = await Publicar(navegador, Ruta, new Dictionary<string, string>
        {
            ["NuevaContrasena"] = PasswordNueva,
            ["ConfirmarContrasena"] = PasswordNueva
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(llamadas);
    }

    [Theory]
    [InlineData("", "ValorQueNoDebeRepoblarse1!", MensajesActivacion.PasswordRequerida)]
    [InlineData("   ", "ValorQueNoDebeRepoblarse1!", MensajesActivacion.PasswordRequerida)]
    [InlineData("ValorQueNoDebeRepoblarse1!", "", MensajesActivacion.ConfirmacionRequerida)]
    public async Task Los_campos_vacios_no_llaman_al_gateway_ni_repueblan_las_contrasenas(
        string nueva, string confirmacion, string esperado)
    {
        var navegador = await AbrirEnlace();

        var respuesta = await EnviarFormulario(navegador, nueva, confirmacion);
        var html = await Texto(respuesta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(esperado, html);
        Assert.DoesNotContain("ValorQueNoDebeRepoblarse1!", html);
        Assert.Contains("name=\"NuevaContrasena\"", html);
        Assert.Empty(llamadas);
    }

    [Theory]
    [InlineData(400, "La contraseña debe tener al menos 8 caracteres.")]
    [InlineData(400, "La confirmación no coincide con la contraseña nueva.")]
    [InlineData(503, "No se pudo completar la activación. Intente nuevamente más tarde.")]
    public async Task Los_mensajes_del_backend_se_muestran_tal_cual_y_el_formulario_sigue_disponible(
        int estado, string mensaje)
    {
        estadoDelGateway = estado;
        cuerpoDelGateway = System.Text.Json.JsonSerializer.Serialize(new { error = mensaje });
        var navegador = await AbrirEnlace();

        var respuesta = await EnviarFormulario(navegador, "debil", "debil");
        var html = await Texto(respuesta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(mensaje, html);
        Assert.Contains("name=\"NuevaContrasena\"", html);
        Assert.DoesNotContain("value=\"debil\"", html);
        Assert.Single(llamadas);
        Assert.True(navegador.Cookies.ContainsKey(ActivarCookie));
        Assert.Equal(HttpStatusCode.OK, (await Obtener(navegador, Ruta)).StatusCode);
    }

    [Fact]
    public async Task Un_enlace_invalido_segun_el_backend_oculta_el_formulario_y_enlaza_al_login()
    {
        estadoDelGateway = 400;
        cuerpoDelGateway = System.Text.Json.JsonSerializer.Serialize(new { error = MensajesActivacion.EnlaceInvalido });
        var navegador = await AbrirEnlace();

        var respuesta = await EnviarFormulario(navegador);
        var html = await Texto(respuesta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(MensajesActivacion.EnlaceInvalido, html);
        Assert.DoesNotContain("name=\"NuevaContrasena\"", html);
        Assert.Contains("href=\"/Auth/Login\"", html);
        Assert.False(navegador.Cookies.ContainsKey(ActivarCookie));
    }

    [Theory]
    [InlineData(Ruta)]
    [InlineData(Ruta + "?token=")]
    [InlineData(Ruta + "?token=%20%20")]
    [InlineData(Ruta + "?token=a&token=b")]
    public async Task Sin_token_o_con_un_token_malformado_se_muestra_el_mensaje_generico_con_enlace_al_login(string ruta)
    {
        var navegador = new Navegador();
        var respuesta = await Obtener(navegador, ruta);
        if (respuesta.StatusCode == HttpStatusCode.Redirect)
            respuesta = await Obtener(navegador, respuesta.Headers.Location!.OriginalString);

        var html = await Texto(respuesta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(MensajesActivacion.EnlaceInvalido, html);
        Assert.DoesNotContain("name=\"NuevaContrasena\"", html);
        Assert.Contains("href=\"/Auth/Login\"", html);
        VerificarCabecerasPrivadas(respuesta);
        Assert.Empty(llamadas);
    }

    [Fact]
    public async Task Un_token_demasiado_largo_se_ignora()
    {
        var navegador = new Navegador();

        var respuesta = await Obtener(navegador, $"{Ruta}?token={new string('a', 5000)}");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.False(navegador.Cookies.ContainsKey(ActivarCookie));
    }

    [Fact]
    public async Task Una_referencia_vencida_pide_abrir_de_nuevo_el_enlace_con_acceso_al_login()
    {
        var navegador = await AbrirEnlace();
        frontend.Services.GetRequiredService<ActivacionesPendientes>().Eliminar(navegador.Cookies[ActivarCookie]);

        var respuesta = await Obtener(navegador, Ruta);
        var html = await Texto(respuesta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Vuelva a abrir el enlace que recibió por correo.", html);
        Assert.DoesNotContain("name=\"NuevaContrasena\"", html);
        Assert.Contains("href=\"/Auth/Login\"", html);
    }

    [Fact]
    public async Task Una_referencia_que_vence_entre_el_formulario_y_el_envio_tambien_pide_abrir_el_enlace()
    {
        var navegador = await AbrirEnlace();
        var formulario = await Obtener(navegador, Ruta);
        var antiforgery = await Antiforgery(formulario);
        frontend.Services.GetRequiredService<ActivacionesPendientes>().Eliminar(navegador.Cookies[ActivarCookie]);

        var respuesta = await Publicar(navegador, Ruta, new Dictionary<string, string>
        {
            ["NuevaContrasena"] = PasswordNueva,
            ["ConfirmarContrasena"] = PasswordNueva,
            ["__RequestVerificationToken"] = antiforgery
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(MensajesActivacion.ReferenciaExpirada, await Texto(respuesta));
        Assert.Empty(llamadas);
    }

    [Fact]
    public async Task La_ruta_de_activacion_no_exige_sesion()
    {
        var respuesta = await Obtener(new Navegador(), Ruta);

        Assert.NotEqual(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public void Las_peticiones_informativas_de_hosting_no_se_registran_para_no_filtrar_el_token_en_la_consulta()
    {
        var registro = frontend.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Microsoft.AspNetCore.Hosting.Diagnostics");

        Assert.False(registro.IsEnabled(LogLevel.Information));
    }

    [Fact]
    public void El_mensaje_de_enlace_invalido_del_Frontend_es_identico_al_del_backend()
    {
        Assert.Equal(ActivarCuentaCasoDeUso.EnlaceInvalido, MensajesActivacion.EnlaceInvalido);
    }
}
