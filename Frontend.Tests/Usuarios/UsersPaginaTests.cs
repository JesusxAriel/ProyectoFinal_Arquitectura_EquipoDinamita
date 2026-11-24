using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Frontend;
using Xunit;

namespace PuntoMarket.Frontend.Tests.Usuarios;

public sealed class UsersPaginaTests : IAsyncLifetime
{
    private WebApplication gateway = null!;
    private WebApplication frontend = null!;
    private Uri baseFrontend = null!;
    private bool correoEnviadoRespuesta = true;
    private bool reenviarDebeFallar;
    private int llamadasReenviar;

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
        var usuarios = new[]
        {
            new { id = 1, username = "admin", nombreCompleto = "Usuario de prueba", correo = "admin@correo.test", rol = "Administrador", activo = true, debeCambiarPassword = false },
            new { id = 2, username = "cajero1", nombreCompleto = "Cajero Uno", correo = "cajero1@correo.test", rol = "Cajero", activo = true, debeCambiarPassword = false }
        };
        gateway.MapGet("/api/usuarios", () => Results.Json(usuarios));
        gateway.MapGet("/api/usuarios/{id:int}", (int id) =>
            usuarios.FirstOrDefault(u => u.id == id) is { } usuario
                ? Results.Json(usuario)
                : Results.NotFound(new { error = "El usuario indicado no existe." }));
        gateway.MapGet("/api/usuarios/roles", () => Results.Json(new[]
        {
            new { id = 1, nombre = "Administrador" },
            new { id = 2, nombre = "Cajero" }
        }));
        gateway.MapPost("/api/usuarios", () => Results.Json(new
        {
            id = 3,
            username = "nuevo.usuario",
            nombreCompleto = "Nuevo Usuario",
            correo = "nuevo@correo.test",
            rol = "Cajero",
            activo = true,
            correoEnviado = correoEnviadoRespuesta
        }));
        gateway.MapPost("/api/usuarios/{id:int}/reenviar-acceso", (int id) =>
        {
            Interlocked.Increment(ref llamadasReenviar);
            if (reenviarDebeFallar)
                return Results.Json(new { error = "Error de correo" }, statusCode: StatusCodes.Status400BadRequest);

            return Results.Json(new
            {
                id,
                correoEnviado = correoEnviadoRespuesta
            });
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

    private async Task<HttpClient> IniciarSesion(string usuario)
    {
        var cliente = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true })
        {
            BaseAddress = baseFrontend
        };
        var pagina = await cliente.GetStringAsync("/Auth/Login");
        var token = ExtraerToken(pagina);
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
    public async Task El_administrador_ve_el_listado_y_no_ve_reenviar_para_si_mismo()
    {
        using var cliente = await IniciarSesion("admin");

        var respuesta = await cliente.GetAsync("/Users");
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("admin@correo.test", html);

        var filas = Regex.Matches(html, "<tr[^>]*>.*?</tr>", RegexOptions.Singleline)
            .Select(m => m.Value)
            .ToList();
        var filaAdmin = filas.FirstOrDefault(f => f.Contains("admin@correo.test"));
        var filaCajero = filas.FirstOrDefault(f => f.Contains("cajero1@correo.test"));

        Assert.NotNull(filaAdmin);
        Assert.NotNull(filaCajero);
        Assert.DoesNotContain("Reenviar acceso", filaAdmin);
        Assert.Contains("Reenviar acceso", filaCajero);
    }

    [Fact]
    public async Task El_cajero_es_redirigido_a_inicio_y_ve_aviso_de_permisos()
    {
        using var cliente = await IniciarSesion("cajero");

        var respuesta = await cliente.GetAsync("/Users");
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/", respuesta.Headers.Location!.OriginalString);

        var inicio = await cliente.GetAsync("/");
        var htmlInicio = await inicio.Content.ReadAsStringAsync();
        Assert.Contains("No tiene permisos para ver esta sección.", WebUtility.HtmlDecode(htmlInicio));
    }

    [Fact]
    public async Task Sin_sesion_se_redirige_al_login()
    {
        using var cliente = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = baseFrontend };

        var respuesta = await cliente.GetAsync("/Users");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Auth/Login", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Crear_usuario_con_correo_enviado_muestra_username_y_enlace_de_activacion()
    {
        correoEnviadoRespuesta = true;
        using var cliente = await IniciarSesion("admin");
        var pagina = await cliente.GetStringAsync("/Users");
        var token = ExtraerToken(pagina);

        var post = await cliente.PostAsync("/Users?handler=Crear", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nombres"] = "Nuevo",
            ["Apellidos"] = "Usuario",
            ["Correo"] = "nuevo@correo.test",
            ["RolId"] = "2",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);

        var html = await cliente.GetStringAsync("/Users");
        Assert.Contains("nuevo.usuario", html);
        Assert.Contains("Se envió un enlace de activación a nuevo@correo.test.", WebUtility.HtmlDecode(html));
    }

    [Fact]
    public async Task Crear_usuario_sin_correo_enviado_muestra_username_y_advertencia_amarilla()
    {
        correoEnviadoRespuesta = false;
        using var cliente = await IniciarSesion("admin");
        var pagina = await cliente.GetStringAsync("/Users");
        var token = ExtraerToken(pagina);

        var post = await cliente.PostAsync("/Users?handler=Crear", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nombres"] = "Nuevo",
            ["Apellidos"] = "Usuario",
            ["Correo"] = "nuevo@correo.test",
            ["RolId"] = "2",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);

        var html = await cliente.GetStringAsync("/Users");
        const string mensaje = "Usuario nuevo.usuario creado, pero no se pudo enviar el correo. Use Reenviar acceso.";
        Assert.Contains(mensaje, html);
        Assert.Matches("class=\"alert alert-warning[^\"]*\"[^>]*>" + Regex.Escape(mensaje) + "<", html);
        Assert.DoesNotMatch("alert-success[^>]*>[^<]*creado", html);
        Assert.DoesNotContain("Se envió un enlace de activación", WebUtility.HtmlDecode(html));
    }

    [Fact]
    public async Task Reenviar_acceso_con_correo_no_enviado_muestra_advertencia_amarilla()
    {
        correoEnviadoRespuesta = false;
        reenviarDebeFallar = false;
        using var cliente = await IniciarSesion("admin");
        var token = ExtraerToken(await cliente.GetStringAsync("/Users"));

        var post = await cliente.PostAsync("/Users?handler=ReenviarAcceso", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = "2",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);

        var html = await cliente.GetStringAsync("/Users");
        const string mensaje = "No se pudo enviar el correo. Use Reenviar acceso.";
        Assert.Matches("class=\"alert alert-warning[^\"]*\"[^>]*>" + Regex.Escape(mensaje) + "<", html);
        Assert.DoesNotMatch("alert-success[^>]*>[^<]*correo", html);
        Assert.DoesNotContain("Se envió un nuevo enlace de activación.", WebUtility.HtmlDecode(html));
        Assert.Equal(1, llamadasReenviar);
    }

    [Fact]
    public async Task Reenviar_acceso_a_la_propia_cuenta_se_rechaza_sin_llamar_a_reenviar()
    {
        using var cliente = await IniciarSesion("admin");
        var token = ExtraerToken(await cliente.GetStringAsync("/Users"));

        var post = await cliente.PostAsync("/Users?handler=ReenviarAcceso", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = "1",
            ["__RequestVerificationToken"] = token
        }));

        var html = await post.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        Assert.Matches("class=\"alert alert-danger[^\"]*\"[^>]*>No puede reenviar el acceso a su propia cuenta\\.<", html);
        Assert.Equal(0, llamadasReenviar);
        Assert.DoesNotContain("Se envió un nuevo enlace de activación.", WebUtility.HtmlDecode(await cliente.GetStringAsync("/Users")));
    }

    [Fact]
    public async Task Reenviar_acceso_exitoso_y_fallido_muestran_mensajes_esperados()
    {
        correoEnviadoRespuesta = true;
        reenviarDebeFallar = false;
        using var cliente = await IniciarSesion("admin");
        var pagina = await cliente.GetStringAsync("/Users");
        var token = ExtraerToken(pagina);

        var okPost = await cliente.PostAsync("/Users?handler=ReenviarAcceso", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = "2",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, okPost.StatusCode);
        Assert.Contains("Se envió un nuevo enlace de activación.", WebUtility.HtmlDecode(await cliente.GetStringAsync("/Users")));

        reenviarDebeFallar = true;
        var pagina2 = await cliente.GetStringAsync("/Users");
        var token2 = ExtraerToken(pagina2);
        var failPost = await cliente.PostAsync("/Users?handler=ReenviarAcceso", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = "2",
            ["__RequestVerificationToken"] = token2
        }));
        Assert.Equal(HttpStatusCode.OK, failPost.StatusCode);
        Assert.Contains("Error de correo", await failPost.Content.ReadAsStringAsync());

        var despues = await cliente.GetAsync("/Users");
        Assert.Equal(HttpStatusCode.OK, despues.StatusCode);
        Assert.DoesNotContain("Error de correo", await despues.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Validacion_muestra_mensajes_en_espanol()
    {
        using var cliente = await IniciarSesion("admin");
        var pagina = await cliente.GetStringAsync("/Users");
        var token = ExtraerToken(pagina);

        var post = await cliente.PostAsync("/Users?handler=Crear", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nombres"] = "",
            ["Apellidos"] = "",
            ["Correo"] = "",
            ["RolId"] = "0",
            ["__RequestVerificationToken"] = token
        }));

        var html = await post.Content.ReadAsStringAsync();
        Assert.Contains("El campo Nombres es obligatorio.", html);
        Assert.Contains("Seleccione un rol válido.", WebUtility.HtmlDecode(html));
    }
}