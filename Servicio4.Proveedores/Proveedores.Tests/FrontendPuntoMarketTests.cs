extern alias Frontend;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ClienteApi = Frontend::Frontend.Demo.Services.ProveedoresApiClient;
using Perfil = Frontend::Frontend.Demo.Services.DemoPerfilSession;
using Tokens = Frontend::Frontend.Demo.Services.DemoTokenProvider;
using Consulta = Frontend::Frontend.Demo.Models.ConsultaVm;
using ProveedorVm = Frontend::Frontend.Demo.Models.ProveedorVm;

namespace Proveedores.Tests;

public class FrontendPuntoMarketTests
{
    private sealed class Gateway : HttpMessageHandler, IHttpClientFactory
    {
        public bool FallaToken { get; set; }
        public bool ConflictoEscritura { get; set; } = true;
        public bool ValidacionEscritura { get; set; }
        public bool FallaSegundaPagina { get; set; }
        public int TokensEmitidos { get; private set; }
        public int Lecturas { get; private set; }
        public HttpClient CreateClient(string name) => new(this, false) { BaseAddress = new Uri("http://gateway.test/") };
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            if (r.RequestUri!.AbsolutePath == "/dev-auth/token")
            {
                TokensEmitidos++;
                return Task.FromResult(FallaToken ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    : Json(new { access_token = "demo", expires_in = 3600 }));
            }
            if (r.Method != HttpMethod.Get && ValidacionEscritura)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new {
                    detail = "Revise el teléfono de la empresa.", code = "Proveedor.NumeroInvalido",
                    errors = new Dictionary<string,string[]> { ["numeroEmpresa"] = ["Use un teléfono válido."] }
                }) });
            if (r.Method != HttpMethod.Get)
                return Task.FromResult(ConflictoEscritura
                    ? new HttpResponseMessage(HttpStatusCode.Conflict) { Content = JsonContent.Create(new { detail = "Cambio concurrente de prueba. Recargue los datos.", code = "Proveedor.CambioConcurrente" }) }
                    : Json(new ProveedorVm("000000000000000000000999", "Nuevo", "1", null, false, true, "admin", null, DateTime.UtcNow, null)));
            Lecturas++;
            var q = QueryHelpers.ParseQuery(r.RequestUri.Query);
            var pagina = int.Parse(q["pagina"].ToString());
            var tamano = int.Parse(q["tamanoPagina"].ToString());
            if (FallaSegundaPagina && pagina == 2) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            var estado = q["estado"].ToString();
            var total = estado == "Activos" ? 133 : estado == "Inactivos" ? 12 : 145;
            var comparador = System.Globalization.CultureInfo.GetCultureInfo("es").CompareInfo;
            var termino = q["busqueda"].ToString();
            bool Coincide(string? v) => v is not null && comparador.IndexOf(v, termino,
                System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0;
            var encontrados = Enumerable.Range(1, total)
                .Select(n => new ProveedorVm(n.ToString("x24"), n == 125 ? "Sofía Test" : $"Proveedor {n}",
                    n == 125 ? "44998877" : $"7{n:D7}", n == 125 ? "unico@correo.test" : $"general{n}@correo.test",
                    false, estado != "Inactivos", "seed", null, DateTime.UtcNow, null))
                .Where(p => Coincide(p.NombreEmpresa) || Coincide(p.NumeroEmpresa) || Coincide(p.CorreoReferencia)).ToList();
            total = encontrados.Count;
            var items = encontrados.Skip((pagina - 1) * tamano).Take(tamano).ToList();
            return Task.FromResult(Json(new { items, pagina, tamanoPagina = tamano, total, totalPaginas = (int)Math.Ceiling(total / (double)tamano) }));
        }
        private static HttpResponseMessage Json(object obj) => new(HttpStatusCode.OK) { Content = JsonContent.Create(obj) };
    }

    private static WebApplicationFactory<Frontend::Program> Fabrica(Gateway gateway) =>
        new WebApplicationFactory<Frontend::Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureLogging(l => l.ClearProviders());
            b.ConfigureTestServices(s =>
            {
                s.RemoveAll<IHttpClientFactory>(); s.AddSingleton<IHttpClientFactory>(gateway);
                s.RemoveAll<IDataProtectionProvider>(); s.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            });
        });

    private static string Csrf(string html) => WebUtility.HtmlDecode(Regex.Match(html,
        "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);

    [Theory]
    [InlineData("Activos", false)]
    [InlineData("Inactivos", true)]
    public async Task Buscador_RenderizaSoloDosOrdenes_MantieneEstadoYDireccion_YBotonLimpiar(string estado, bool descendente)
    {
        using var gateway = new Gateway(); await using var fabrica = Fabrica(gateway); using var cliente = fabrica.CreateClient();
        using var login = await Login(cliente, "Administrador");
        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync($"/Proveedores?Estado={estado}&Descendente={descendente}&Busqueda=com"));
        var select = Regex.Match(html,"<select id=\"OrdenarPor\"[^>]*>(.*?)</select>",RegexOptions.Singleline).Value;
        Assert.Equal(2,Regex.Matches(select,"<option").Count);
        Assert.Contains("Nombre A-Z",select); Assert.Contains("Nombre Z-A",select);
        Assert.DoesNotContain("id=\"Descendente\"",html);
        Assert.Contains("name=\"Descendente\"",select);
        Assert.Matches($"<option[^>]*value=\"{descendente.ToString().ToLowerInvariant()}\"[^>]*selected",select);
        Assert.Contains($"name=\"Estado\" value=\"{estado}\"",html);
        Assert.Contains("data-live-search",html); Assert.Contains("data-search-clear",html);
        Assert.Contains("proveedores-busqueda.mjs",html);
        Assert.Contains("8 dígitos. Debe comenzar por 4, 6 o 7.",html);
        Assert.DoesNotContain("Prefijo +591",html);
        using var modulo = await cliente.GetAsync("/js/proveedores-busqueda.mjs");
        Assert.Equal(HttpStatusCode.OK,modulo.StatusCode);
    }

    [Theory]
    [InlineData("Crear")]
    [InlineData("Editar")]
    public async Task ValidacionApi_ResaltaCampo_ConservaEntradaCodificada_YReabreModal(string handler)
    {
        using var gateway = new Gateway { ValidacionEscritura = true };
        await using var fabrica = Fabrica(gateway); using var cliente = fabrica.CreateClient();
        using var login = await Login(cliente, "Administrador");
        var inicial = await cliente.GetStringAsync("/Proveedores");
        using var r = await cliente.PostAsync($"/Proveedores?handler={handler}&id=000000000000000000000001",
            new FormUrlEncodedContent(new Dictionary<string,string> {
                ["__RequestVerificationToken"] = Csrf(inicial), ["Form.NombreEmpresa"] = "<script>alert(1)</script>",
                ["Form.NumeroEmpresa"] = "adaded", ["Form.CorreoReferencia"] = "ventas@empresa.com"
            }));
        var html = await r.Content.ReadAsStringAsync();
        Assert.Contains("data-auto-open=\"true\"", html);
        Assert.Contains("value=\"adaded\"", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("Use un tel", WebUtility.HtmlDecode(html));
        Assert.Matches("class=\"form-control is-invalid[^\"]*\"[^>]*id=\"Form_NumeroEmpresa\"", html);
    }

    [Theory]
    [InlineData("Pagina=abc")]
    [InlineData("Pagina=9999999999999999999999")]
    [InlineData("Descendente=incorrecto")]
    public async Task ParametroFrontendInvalido_MensajeControlado_SinConsultaListado(string query)
    {
        using var gateway = new Gateway(); await using var fabrica = Fabrica(gateway); using var cliente = fabrica.CreateClient();
        using var login = await Login(cliente, "Administrador");
        var antes = gateway.Lecturas;
        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Proveedores?" + query));
        Assert.Contains("Revise los parámetros", html);
        Assert.Equal(antes, gateway.Lecturas);
    }

    [Theory]
    [InlineData("Editar")]
    [InlineData("Desactivar")]
    [InlineData("Reactivar")]
    public async Task IdAusenteFrontend_400Controlado(string handler)
    {
        using var gateway = new Gateway(); await using var fabrica = Fabrica(gateway); using var cliente = fabrica.CreateClient();
        using var login = await Login(cliente, "Administrador");
        var inicial = await cliente.GetStringAsync("/Proveedores");
        using var r = await cliente.PostAsync("/Proveedores?handler=" + handler,
            new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = Csrf(inicial) }));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("identificador", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BooleanoMalformadoFrontend_NoSeConvierteSilenciosamenteEnFalse()
    {
        using var gateway = new Gateway { ConflictoEscritura = false }; await using var fabrica = Fabrica(gateway); using var cliente = fabrica.CreateClient();
        using var login = await Login(cliente, "Administrador");
        var inicial = await cliente.GetStringAsync("/Proveedores");
        using var r = await cliente.PostAsync("/Proveedores?handler=Crear",
            new FormUrlEncodedContent(new Dictionary<string,string> {
                ["__RequestVerificationToken"] = Csrf(inicial), ["Form.NombreEmpresa"] = "Conservar",
                ["Form.NumeroEmpresa"] = "71234567", ["Form.EsAutogestionado"] = "incorrecto"
            }));
        var html = WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());
        Assert.Contains("data-auto-open=\"true\"", html);
        Assert.Contains("Revise los tipos y formatos", html);
        Assert.Contains("value=\"Conservar\"", html);
    }
    private static async Task<HttpResponseMessage> Login(HttpClient c, string perfil)
    {
        var html = await c.GetStringAsync("/Login");
        return await c.PostAsync("/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["perfilElegido"] = perfil, ["__RequestVerificationToken"] = Csrf(html) }));
    }

    [Fact]
    public async Task SinPerfil_RedirigeALogin_SinConsultarApi()
    {
        using var gateway = new Gateway(); await using var fabrica = Fabrica(gateway);
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        foreach (var ruta in new[] { "/", "/Index", "/Proveedores" })
        {
            using var r = await cliente.GetAsync(ruta);
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal("/Login", r.Headers.Location?.OriginalString);
        }
        Assert.Equal(0, gateway.TokensEmitidos); Assert.Equal(0, gateway.Lecturas);
        var login = await cliente.GetStringAsync("/Login");
        Assert.Contains("Bienvenido a", login);
        Assert.DoesNotContain("id=\"navegacion\"", login);
    }

    [Fact]
    public async Task Admin_HomeUsaMetadatos_YLogoutBorraPerfil()
    {
        using var gateway = new Gateway(); await using var fabrica = Fabrica(gateway);
        using var cliente = fabrica.CreateClient();
        using var login = await Login(cliente, "Administrador");
        var home = await login.Content.ReadAsStringAsync();
        Assert.Contains("data-kpi=\"total\">145", home);
        Assert.Contains("data-kpi=\"activos\">133", home);
        Assert.Contains("data-kpi=\"eliminados\">12", home);
        Assert.Equal(1, gateway.TokensEmitidos);
        using var logout = await cliente.PostAsync("/Logout", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Csrf(home) }));
        Assert.Equal("/Login", logout.RequestMessage!.RequestUri!.AbsolutePath);
        using var sinPerfil = await cliente.GetAsync("/Proveedores");
        Assert.Equal("/Login", sinPerfil.RequestMessage!.RequestUri!.AbsolutePath);
    }

    [Theory]
    [InlineData("Cajero", false)]
    [InlineData("Administrador", true)]
    public async Task Roles_AccionesVisibles_YProteccionDeHandlers(string perfil, bool administrador)
    {
        using var gateway = new Gateway(); await using var fabrica = Fabrica(gateway); using var cliente = fabrica.CreateClient();
        using var login = await Login(cliente, perfil);
        var html = await cliente.GetStringAsync("/Proveedores");
        Assert.Equal(administrador, html.Contains("id=\"proveedorModal\""));
        Assert.Equal(administrador, html.Contains("id=\"eliminarModal\""));
        var eliminados = await cliente.GetStringAsync("/Proveedores?Estado=Inactivos");
        Assert.Equal(administrador, eliminados.Contains("Reactivar</button>"));
        if (!administrador)
        {
            foreach (var handler in new[] { "Crear", "Editar", "Desactivar", "Reactivar" })
            {
                using var r = await cliente.PostAsync($"/Proveedores?handler={handler}&id=abc", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Csrf(html) }));
                Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
            }
        }
    }

    [Theory]
    [InlineData("otro")]
    [InlineData("")]
    public async Task LoginNoAceptaPerfilesArbitrarios(string perfil)
    {
        using var gateway = new Gateway(); await using var fabrica = Fabrica(gateway); using var cliente = fabrica.CreateClient();
        using var r = await Login(cliente, perfil);
        Assert.Equal("/Login", r.RequestMessage!.RequestUri!.AbsolutePath);
        Assert.Equal(0, gateway.TokensEmitidos);
    }

    [Fact]
    public async Task EmisorNoDisponible_NoActivaSesion_NiMuestraTotalesFicticios()
    {
        using var gateway = new Gateway { FallaToken = true }; await using var fabrica = Fabrica(gateway); using var cliente = fabrica.CreateClient();
        using var r = await Login(cliente, "Administrador");
        Assert.Equal("/Login", r.RequestMessage!.RequestUri!.AbsolutePath);
        Assert.Contains("No se pudo iniciar", await r.Content.ReadAsStringAsync());
        using var protegido = await cliente.GetAsync("/Index");
        Assert.Equal("/Login", protegido.RequestMessage!.RequestUri!.AbsolutePath);
    }

    [Theory]
    [InlineData("unico@correo", "Activos")]
    [InlineData("sofia", "Activos")]
    [InlineData("44998877", "Activos")]
    public async Task BusquedaIncluyeSegundaPagina_CorreoNombreYTelefono(string termino, string estado)
    {
        using var gateway = new Gateway(); using var cache = new MemoryCache(new MemoryCacheOptions());
        var api = new ClienteApi(gateway, new Perfil(new HttpContextAccessor()), new Tokens(gateway, cache));
        var r = await api.BuscarAsync(new Consulta(termino, estado, "Nombre", false, 1, 10), default);
        Assert.True(r.Ok); Assert.Equal(1, r.Data!.Total); Assert.Single(r.Data.Items);
        Assert.Equal("Sofía Test", r.Data.Items[0].NombreEmpresa);
        Assert.Equal(1, gateway.Lecturas);
    }

    [Fact]
    public async Task BusquedaNoEntregaResultadosParcialesSiFallaOtraPagina()
    {
        using var gateway = new Gateway { FallaSegundaPagina = true }; using var cache = new MemoryCache(new MemoryCacheOptions());
        var api = new ClienteApi(gateway, new Perfil(new HttpContextAccessor()), new Tokens(gateway, cache));
        var r = await api.BuscarAsync(new Consulta("proveedor", "Activos", "Nombre", false, 2, 10), default);
        Assert.False(r.Ok); Assert.Null(r.Data); Assert.Equal(503, r.Status);
    }
    [Theory]
    [InlineData("Crear")]
    [InlineData("Editar")]
    public async Task ConflictoAlGuardar_ConservaCampos_ReabreModal_YMuestraError(string handler)
    {
        using var gateway = new Gateway(); await using var fabrica = Fabrica(gateway); using var cliente = fabrica.CreateClient();
        using var login = await Login(cliente, "Administrador");
        var inicial = await cliente.GetStringAsync("/Proveedores");
        using var r = await cliente.PostAsync($"/Proveedores?handler={handler}&id=000000000000000000000001", new FormUrlEncodedContent(new Dictionary<string,string>
        {
            ["__RequestVerificationToken"] = Csrf(inicial), ["Form.NombreEmpresa"] = "Datos conservados",
            ["Form.NumeroEmpresa"] = "123456", ["Form.CorreoReferencia"] = "conservado@correo.test"
        }));
        var html = WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("data-auto-open=\"true\"", html);
        Assert.Contains("value=\"Datos conservados\"", html);
        Assert.Contains("value=\"123456\"", html);
        Assert.Contains("value=\"conservado@correo.test\"", html);
        Assert.Contains("Cambio concurrente de prueba", html);
        Assert.Contains(handler == "Crear" ? "Añadir proveedor" : "Guardar cambios", html);
    }

    [Fact]
    public async Task CrearDesdeEliminados_RedirigeAActivos_YConservaTamano()
    {
        using var gateway = new Gateway { ConflictoEscritura = false }; await using var fabrica = Fabrica(gateway); using var cliente = fabrica.CreateClient();
        using var login = await Login(cliente, "Administrador");
        var inicial = await cliente.GetStringAsync("/Proveedores?Estado=Inactivos&TamanoPagina=25");
        using var r = await cliente.PostAsync("/Proveedores?handler=Crear&Estado=Inactivos&TamanoPagina=25", new FormUrlEncodedContent(new Dictionary<string,string>
        {
            ["__RequestVerificationToken"] = Csrf(inicial), ["Form.NombreEmpresa"] = "Nuevo", ["Form.NumeroEmpresa"] = "1"
        }));
        var q = QueryHelpers.ParseQuery(r.RequestMessage!.RequestUri!.Query);
        Assert.Equal("Activos", q["Estado"].ToString());
        Assert.Equal("25", q["TamanoPagina"].ToString());
        Assert.Equal("1", q["Pagina"].ToString());
    }
}
