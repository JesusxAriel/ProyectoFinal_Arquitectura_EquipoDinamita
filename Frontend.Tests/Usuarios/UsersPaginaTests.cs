using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    private readonly List<(int Id, bool Activo)> cambiosDeEstado = [];
    private readonly List<(int Id, string Nombres, string Apellidos, string Correo, int RolId)> actualizaciones = [];
    private bool crearDebeFallar;

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
            new { id = 2, username = "cajero1", nombreCompleto = "Cajero Uno", correo = "cajero1@correo.test", rol = "Cajero", activo = true, debeCambiarPassword = false },
            new { id = 4, username = "inactivo1", nombreCompleto = "Inactivo Uno", correo = "inactivo1@correo.test", rol = "Cajero", activo = false, debeCambiarPassword = false }
        };
        gateway.MapGet("/api/usuarios", () => Results.Json(usuarios));
        gateway.MapGet("/api/usuarios/{id:int}", (int id) =>
            usuarios.FirstOrDefault(u => u.id == id) is { } usuario
                ? Results.Json(usuario)
                : Results.NotFound(new { error = "El usuario indicado no existe." }));
        gateway.MapMethods("/api/usuarios/{id:int}/estado", ["PATCH"], async (int id, HttpContext contexto) =>
        {
            var cuerpo = await contexto.Request.ReadFromJsonAsync<Dictionary<string, bool>>();
            lock (cambiosDeEstado)
                cambiosDeEstado.Add((id, cuerpo!["activo"]));
            return Results.NoContent();
        });
        gateway.MapGet("/api/usuarios/roles", () => Results.Json(new[]
        {
            new { id = 1, nombre = "Administrador" },
            new { id = 2, nombre = "Cajero" }
        }));
        gateway.MapPost("/api/usuarios", () => crearDebeFallar
            ? Results.Json(new { error = "Ya existe un usuario con ese correo." }, statusCode: StatusCodes.Status400BadRequest)
            : Results.Json(new
            {
                id = 3,
                username = "nuevo.usuario",
                nombreCompleto = "Nuevo Usuario",
                correo = "nuevo@correo.test",
                rol = "Cajero",
                activo = true,
                correoEnviado = correoEnviadoRespuesta
            }));
        gateway.MapPut("/api/usuarios/{id:int}", async (int id, HttpContext contexto) =>
        {
            var cuerpo = await contexto.Request.ReadFromJsonAsync<JsonElement>();
            lock (actualizaciones)
                actualizaciones.Add((
                    id,
                    cuerpo.GetProperty("nombres").GetString()!,
                    cuerpo.GetProperty("apellidos").GetString()!,
                    cuerpo.GetProperty("correo").GetString()!,
                    cuerpo.GetProperty("rolId").GetInt32()));
            return Results.Json(usuarios.First(u => u.id == id));
        });
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

    private static string FilaDe(string html, string correo) =>
        Regex.Matches(html, "<tr[^>]*>.*?</tr>", RegexOptions.Singleline)
            .Select(m => m.Value)
            .Single(f => f.Contains(correo));

    private static string FormularioDeEstado(string fila) =>
        Regex.Match(fila, "<form[^>]*handler=CambiarEstado[^>]*>.*?</form>", RegexOptions.Singleline).Value;

    private static string ValorQueEnviaElNavegador(string formulario, string campo)
    {
        var entrada = Regex.Matches(formulario, "<input type=\"hidden\" name=\"" + campo + "\"[^>]*>")
            .Select(m => m.Value)
            .Single();
        var valor = Regex.Match(entrada, "value=\"([^\"]*)\"");
        return valor.Success ? valor.Groups[1].Value : "";
    }

    [Fact]
    public async Task El_boton_Activar_de_un_usuario_inactivo_envia_activo_true_y_el_adaptador_recibe_true()
    {
        using var cliente = await IniciarSesion("admin");
        var pagina = await cliente.GetStringAsync("/Users");
        var fila = FilaDe(pagina, "inactivo1@correo.test");
        Assert.Matches(@">\s*Activar\s*<", fila);

        var formulario = FormularioDeEstado(fila);
        var activo = ValorQueEnviaElNavegador(formulario, "activo");
        Assert.Equal("true", activo);

        var post = await cliente.PostAsync("/Users?handler=CambiarEstado", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = ValorQueEnviaElNavegador(formulario, "id"),
            ["activo"] = activo,
            ["__RequestVerificationToken"] = ExtraerToken(pagina)
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal([(4, true)], cambiosDeEstado);
        Assert.Contains("Usuario activado correctamente.", WebUtility.HtmlDecode(await cliente.GetStringAsync("/Users")));
    }

    [Fact]
    public async Task El_boton_Desactivar_de_un_usuario_activo_envia_activo_false_y_el_adaptador_recibe_false()
    {
        using var cliente = await IniciarSesion("admin");
        var pagina = await cliente.GetStringAsync("/Users");
        var fila = FilaDe(pagina, "cajero1@correo.test");
        Assert.Matches(@">\s*Desactivar\s*<", fila);

        var formulario = FormularioDeEstado(fila);
        var activo = ValorQueEnviaElNavegador(formulario, "activo");
        Assert.Equal("false", activo);

        var post = await cliente.PostAsync("/Users?handler=CambiarEstado", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = ValorQueEnviaElNavegador(formulario, "id"),
            ["activo"] = activo,
            ["__RequestVerificationToken"] = ExtraerToken(pagina)
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal([(2, false)], cambiosDeEstado);
        Assert.Contains("Usuario desactivado correctamente.", WebUtility.HtmlDecode(await cliente.GetStringAsync("/Users")));
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
    public async Task Crear_usuario_con_correo_enviado_muestra_username_y_aviso_de_credenciales()
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
        Assert.Contains("Se envió un correo con sus credenciales de acceso a nuevo@correo.test.", WebUtility.HtmlDecode(html));
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
        Assert.Matches("class=\"alert alert-warning[^\"]*\"[^>]*>\\s*" + Regex.Escape(mensaje) + "\\s*<button", html);
        Assert.DoesNotMatch("alert-success[^>]*>[^<]*creado", html);
        Assert.DoesNotContain("Se envió un correo con sus credenciales de acceso", WebUtility.HtmlDecode(html));
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
        Assert.Matches("class=\"alert alert-warning[^\"]*\"[^>]*>\\s*" + Regex.Escape(mensaje) + "\\s*<button", html);
        Assert.DoesNotMatch("alert-success[^>]*>[^<]*correo", html);
        Assert.DoesNotContain("Se envió un correo con un nuevo acceso temporal.", WebUtility.HtmlDecode(html));
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
        Assert.Matches("class=\"alert alert-danger[^\"]*\"[^>]*>\\s*No puede reenviar el acceso a su propia cuenta\\.\\s*<button", html);
        Assert.Equal(0, llamadasReenviar);
        Assert.DoesNotContain("Se envió un correo con un nuevo acceso temporal.", WebUtility.HtmlDecode(await cliente.GetStringAsync("/Users")));
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
        Assert.Contains("Se envió un correo con un nuevo acceso temporal.", WebUtility.HtmlDecode(await cliente.GetStringAsync("/Users")));

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

    private static string CampoInvalido(string etiqueta, string nombre) =>
        "<" + etiqueta + @"(?=[^>]*\bname=""" + nombre + @""")[^>]*\bclass=""[^""]*\bis-invalid\b[^""]*""";

    private static string MensajeDelCampo(string nombre, string mensaje) =>
        @"<span(?=[^>]*\bdata-valmsg-for=""" + nombre + @""")(?=[^>]*\bclass=""[^""]*\binvalid-feedback\b[^""]*"")[^>]*>" + mensaje + "</span>";

    private static async Task<HttpResponseMessage> Publicar(HttpClient cliente, string ruta, Dictionary<string, string> campos)
    {
        var token = ExtraerToken(await cliente.GetStringAsync("/Users"));
        campos["__RequestVerificationToken"] = token;
        return await cliente.PostAsync(ruta, new FormUrlEncodedContent(campos));
    }

    [Fact]
    public async Task El_aviso_de_exito_se_cierra_solo_a_los_6_segundos_y_tiene_boton_de_cerrar()
    {
        correoEnviadoRespuesta = true;
        using var cliente = await IniciarSesion("admin");
        await Publicar(cliente, "/Users?handler=Crear", new()
        {
            ["Nombres"] = "Nuevo", ["Apellidos"] = "Usuario", ["Correo"] = "nuevo@correo.test", ["RolId"] = "2"
        });

        var html = await cliente.GetStringAsync("/Users");

        Assert.Matches("<div class=\"alert alert-success[^\"]*alert-dismissible[^\"]*fade show[^\"]*\" role=\"status\" data-aviso-ms=\"6000\">", html);
        Assert.Matches("data-bs-dismiss=\"alert\" aria-label=\"Cerrar aviso\"", html);
    }

    [Fact]
    public async Task Los_avisos_de_advertencia_y_de_error_duran_10_segundos_y_conservan_su_rol()
    {
        correoEnviadoRespuesta = false;
        using var cliente = await IniciarSesion("admin");
        await Publicar(cliente, "/Users?handler=Crear", new()
        {
            ["Nombres"] = "Nuevo", ["Apellidos"] = "Usuario", ["Correo"] = "nuevo@correo.test", ["RolId"] = "2"
        });
        var advertencia = await cliente.GetStringAsync("/Users");
        var error = await (await Publicar(cliente, "/Users?handler=ReenviarAcceso", new() { ["id"] = "1" })).Content.ReadAsStringAsync();

        Assert.Matches("<div class=\"alert alert-warning[^\"]*alert-dismissible[^\"]*fade show[^\"]*\" role=\"status\" data-aviso-ms=\"10000\">", advertencia);
        Assert.Matches("<div class=\"alert alert-danger[^\"]*alert-dismissible[^\"]*fade show[^\"]*\" role=\"alert\" data-aviso-ms=\"10000\">", error);
        Assert.Contains("data-bs-dismiss=\"alert\"", advertencia);
        Assert.Contains("data-bs-dismiss=\"alert\"", error);
    }

    [Fact]
    public async Task Sin_avisos_la_pagina_no_dibuja_alertas_ni_campos_con_error()
    {
        using var cliente = await IniciarSesion("admin");

        var html = await cliente.GetStringAsync("/Users");

        Assert.DoesNotContain("data-aviso-ms", html);
        Assert.DoesNotContain("is-invalid", html);
    }

    [Fact]
    public async Task Cada_campo_con_error_lleva_is_invalid_y_su_mensaje_enlazado_por_nombre()
    {
        using var cliente = await IniciarSesion("admin");

        var respuesta = await Publicar(cliente, "/Users?handler=Crear", new()
        {
            ["Nombres"] = "", ["Apellidos"] = "", ["Correo"] = "no-es-un-correo", ["RolId"] = "0"
        });
        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Matches(CampoInvalido("input", "Nombres"), html);
        Assert.Matches(CampoInvalido("input", "Apellidos"), html);
        Assert.Matches(CampoInvalido("input", "Correo"), html);
        Assert.Matches(CampoInvalido("select", "RolId"), html);
        Assert.Matches(MensajeDelCampo("Nombres", @"El campo Nombres es obligatorio\."), html);
        Assert.Contains("data-valmsg-for=\"Apellidos\"", html);
        Assert.Contains("data-valmsg-for=\"Correo\"", html);
        Assert.Contains("data-valmsg-for=\"RolId\"", html);
        Assert.Contains("El campo Correo no es una dirección de correo válida.", html);
    }

    [Fact]
    public async Task La_pagina_carga_los_scripts_de_limpieza_y_de_avisos_y_su_hoja_de_estilos()
    {
        using var cliente = await IniciarSesion("admin");

        var html = await cliente.GetStringAsync("/Users");

        Assert.Contains("/js/limpiar-validacion.mjs", html);
        Assert.Contains("/js/avisos-temporales.mjs", html);
        Assert.Contains("/css/usuarios.css", html);
    }

    private static void AfirmarModalAbierto(string html, string titulo, string accion)
    {
        Assert.Contains("id=\"usuarioModal\"", html);
        Assert.Contains("data-auto-open=\"true\"", html);
        Assert.Contains("id=\"usuarioModalTitulo\">" + titulo + "</h2>", html);
        Assert.Matches(@"<form[^>]*action=""/Users\?handler=" + accion + @"""", html);
        Assert.Contains("data-bs-dismiss=\"modal\">Cancelar</button>", html);
    }

    private static string ValorDelCampo(string nombre, string valor) =>
        @"<input(?=[^>]*\bname=""" + nombre + @""")[^>]*\bvalue=""" + valor + @"""";

    private static string OpcionElegida(string valor, string texto) =>
        @"<option(?=[^>]*\bselected)(?=[^>]*\bvalue=""" + valor + @""")[^>]*>" + texto + "</option>";

    [Fact]
    public async Task El_listado_no_dibuja_formularios_ni_modal_y_ofrece_el_boton_Crear_usuario()
    {
        using var cliente = await IniciarSesion("admin");

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Users"));

        Assert.Contains("class=\"catalogo usuarios\"", html);
        Assert.Matches(@"<form[^>]*action=""/Users\?handler=Nuevo""[^>]*>[\s\S]*?Crear usuario[\s\S]*?</form>", html);
        Assert.Contains("Listado de usuarios", html);
        Assert.DoesNotContain("id=\"usuarioModal\"", html);
        Assert.DoesNotContain("name=\"Nombres\"", html);
        Assert.DoesNotContain("name=\"Correo\"", html);
        Assert.Contains("/js/catalogos-modales.mjs", html);
        Assert.Contains("/css/catalogos.css", html);
    }

    [Fact]
    public async Task Crear_usuario_abre_el_modal_con_el_formulario_vacio_y_los_roles()
    {
        using var cliente = await IniciarSesion("admin");

        var respuesta = await Publicar(cliente, "/Users?handler=Nuevo", []);
        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        AfirmarModalAbierto(html, "Crear usuario", "Crear");
        Assert.Contains("name=\"Nombres\"", html);
        Assert.Contains("name=\"Apellidos\"", html);
        Assert.Contains("name=\"Correo\"", html);
        Assert.Matches(OpcionElegida("0", "Seleccione un rol"), html);
        Assert.Contains(">Cajero</option>", html);
        Assert.DoesNotMatch(OpcionElegida("2", "Cajero"), html);
        Assert.DoesNotContain("is-invalid", html);
        Assert.DoesNotContain("Nombre actual", html);
    }

    [Fact]
    public async Task Los_errores_de_validacion_al_crear_reabren_el_modal_con_lo_escrito_y_los_mensajes_en_espanol()
    {
        using var cliente = await IniciarSesion("admin");

        var respuesta = await Publicar(cliente, "/Users?handler=Crear", new()
        {
            ["Nombres"] = "Ana María", ["Apellidos"] = "", ["Correo"] = "ana@correo.test", ["RolId"] = "2"
        });
        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        AfirmarModalAbierto(html, "Crear usuario", "Crear");
        Assert.Matches(ValorDelCampo("Nombres", "Ana María"), html);
        Assert.Matches(ValorDelCampo("Correo", "ana@correo.test"), html);
        Assert.Matches(OpcionElegida("2", "Cajero"), html);
        Assert.Matches(CampoInvalido("input", "Apellidos"), html);
        Assert.DoesNotMatch(CampoInvalido("input", "Nombres"), html);
        Assert.Matches(MensajeDelCampo("Apellidos", @"El campo Apellidos es obligatorio\."), html);
        Assert.Empty(cambiosDeEstado);
    }

    [Fact]
    public async Task Si_el_servidor_rechaza_la_creacion_el_modal_se_reabre_con_el_error_dentro_y_lo_escrito()
    {
        crearDebeFallar = true;
        using var cliente = await IniciarSesion("admin");

        var respuesta = await Publicar(cliente, "/Users?handler=Crear", new()
        {
            ["Nombres"] = "Luis", ["Apellidos"] = "Mamani", ["Correo"] = "luis@correo.test", ["RolId"] = "1"
        });
        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        AfirmarModalAbierto(html, "Crear usuario", "Crear");
        Assert.Matches(@"id=""usuarioModal""[\s\S]*<div class=""alert alert-danger"" role=""alert"">Ya existe un usuario con ese correo\.</div>", html);
        Assert.DoesNotContain("data-aviso-ms=\"10000\"", html);
        Assert.Matches(ValorDelCampo("Nombres", "Luis"), html);
        Assert.Matches(ValorDelCampo("Apellidos", "Mamani"), html);
        Assert.Matches(OpcionElegida("1", "Administrador"), html);
    }

    [Fact]
    public async Task Editar_abre_el_modal_con_correo_rol_y_nombre_actual_precargados()
    {
        using var cliente = await IniciarSesion("admin");

        var respuesta = await Publicar(cliente, "/Users?handler=Editar", new() { ["id"] = "2" });
        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        AfirmarModalAbierto(html, "Editar usuario", "Guardar");
        Assert.Matches(ValorDelCampo("Correo", "cajero1@correo.test"), html);
        Assert.Matches(OpcionElegida("2", "Cajero"), html);
        Assert.Matches(ValorDelCampo("EditarId", "2"), html);
        Assert.Contains("Nombre actual: <strong>Cajero Uno</strong>", html);
        Assert.Contains("Guardar cambios", html);
        Assert.DoesNotContain("is-invalid", html);
    }

    [Fact]
    public async Task Los_errores_al_guardar_reabren_el_modal_de_edicion_con_los_datos_y_el_id()
    {
        using var cliente = await IniciarSesion("admin");

        var respuesta = await Publicar(cliente, "/Users?handler=Guardar", new()
        {
            ["EditarId"] = "2", ["Nombres"] = "", ["Apellidos"] = "Uno", ["Correo"] = "no-es-correo", ["RolId"] = "2"
        });
        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        AfirmarModalAbierto(html, "Editar usuario", "Guardar");
        Assert.Matches(ValorDelCampo("EditarId", "2"), html);
        Assert.Matches(ValorDelCampo("Apellidos", "Uno"), html);
        Assert.Matches(ValorDelCampo("Correo", "no-es-correo"), html);
        Assert.Matches(MensajeDelCampo("Nombres", @"El campo Nombres es obligatorio\."), html);
        Assert.Matches(MensajeDelCampo("Correo", @"El campo Correo no es una dirección de correo válida\."), html);
        Assert.Empty(actualizaciones);
    }

    [Fact]
    public async Task Guardar_los_cambios_envia_los_datos_al_adaptador_y_vuelve_al_listado_con_aviso()
    {
        using var cliente = await IniciarSesion("admin");

        var respuesta = await Publicar(cliente, "/Users?handler=Guardar", new()
        {
            ["EditarId"] = "2", ["Nombres"] = "Cajero", ["Apellidos"] = "Dos", ["Correo"] = "cajero2@correo.test", ["RolId"] = "1"
        });

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal([(2, "Cajero", "Dos", "cajero2@correo.test", 1)], actualizaciones);
        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Users"));
        Assert.Contains("Cambios guardados correctamente.", html);
        Assert.DoesNotContain("id=\"usuarioModal\"", html);
    }

    [Fact]
    public async Task La_fila_del_usuario_actual_solo_ofrece_Editar()
    {
        using var cliente = await IniciarSesion("admin");

        var fila = FilaDe(await cliente.GetStringAsync("/Users"), "admin@correo.test");

        Assert.Contains(">Editar</button>", fila);
        Assert.Contains("Usted", fila);
        Assert.DoesNotContain("Reenviar acceso", fila);
        Assert.DoesNotContain("Desactivar", fila);
        Assert.DoesNotMatch(@">\s*Activar\s*<", fila);
        Assert.DoesNotContain("handler=CambiarEstado", fila);
    }

    [Fact]
    public async Task El_cajero_no_puede_abrir_los_modales_y_es_redirigido_al_inicio()
    {
        using var cliente = await IniciarSesion("cajero");
        var token = ExtraerToken(await cliente.GetStringAsync("/"));

        foreach (var ruta in new[] { "/Users?handler=Nuevo", "/Users?handler=Editar" })
        {
            var respuesta = await cliente.PostAsync(ruta, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["id"] = "2",
                ["__RequestVerificationToken"] = token
            }));

            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.Equal("/", respuesta.Headers.Location!.OriginalString);
        }
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