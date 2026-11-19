extern alias Frontend;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using DemoTokenProvider = Frontend::Frontend.Demo.Services.DemoTokenProvider;
using DemoPerfilSession = Frontend::Frontend.Demo.Services.DemoPerfilSession;
using ProveedoresApiClient = Frontend::Frontend.Demo.Services.ProveedoresApiClient;
using ProveedorFormVm = Frontend::Frontend.Demo.Models.ProveedorFormVm;

namespace Proveedores.Tests;

public class FrontendTokenTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> enviar) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => enviar(request);
    }

    private sealed class Fabrica(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("http://gateway.test/") };
    }

    private static HttpResponseMessage Json(string cuerpo) => new(HttpStatusCode.OK)
        { Content = new StringContent(cuerpo, System.Text.Encoding.UTF8, "application/json") };

    private static ProveedoresApiClient Cliente(Fabrica fabrica, DemoTokenProvider tokens) => new(
        fabrica, new DemoPerfilSession(new HttpContextAccessor()), tokens); // Sin sesión usa Cajero.

    [Fact]
    public async Task Unauthorized_RenuevaYReenviaElMismoCuerpoUnaVez()
    {
        var emitidos = 0;
        var cuerpos = new List<string>();
        var autorizaciones = new List<string?>();
        using var handler = new Handler(async r =>
        {
            if (r.RequestUri!.AbsolutePath == "/dev-auth/token")
                return Json($$"""{"access_token":"token-{{++emitidos}}","expires_in":3600}""");
            cuerpos.Add(await r.Content!.ReadAsStringAsync());
            autorizaciones.Add(r.Headers.Authorization?.Parameter);
            return new HttpResponseMessage(cuerpos.Count == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.Created)
                { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") };
        });
        var fabrica = new Fabrica(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var tokens = new DemoTokenProvider(fabrica, cache);
        var cliente = Cliente(fabrica, tokens);
        var r = await cliente.CrearAsync(new ProveedorFormVm { NombreEmpresa = "Demo", NumeroEmpresa = "1" }, default);
        Assert.True(r.Ok);
        Assert.Equal(2, emitidos);
        Assert.Equal(new[] { "token-1", "token-2" }, autorizaciones);
        Assert.Equal(cuerpos[0], cuerpos[1]);
        Assert.Contains("Demo", cuerpos[1]);
        Assert.Equal("token-2", await tokens.ObtenerAsync("Cajero", default));
        Assert.Equal(2, emitidos);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, 2, 2)]
    [InlineData(HttpStatusCode.Forbidden, 1, 1)]
    [InlineData(HttpStatusCode.InternalServerError, 1, 1)]
    public async Task ReintentoAcotado_YNoRepiteOtrosErrores(HttpStatusCode status, int llamadas, int tokensEsperados)
    {
        var emitidos = 0;
        var enviadas = 0;
        using var handler = new Handler(r => Task.FromResult(r.RequestUri!.AbsolutePath == "/dev-auth/token"
            ? Json($$"""{"access_token":"t{{++emitidos}}","expires_in":3600}""")
            : Responder()));
        HttpResponseMessage Responder() { enviadas++; return new(status); }
        var fabrica = new Fabrica(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new DemoTokenProvider(fabrica, cache);
        var r = await Cliente(fabrica, provider).DesactivarAsync("id", default);
        Assert.False(r.Ok);
        Assert.Equal((int)status, r.Status);
        Assert.Equal(llamadas, enviadas);
        Assert.Equal(tokensEsperados, emitidos);
        await provider.ObtenerAsync("Cajero", default);
        Assert.Equal(tokensEsperados + (status == HttpStatusCode.Unauthorized ? 1 : 0), emitidos);
    }

    [Fact]
    public async Task RenovacionesConcurrentes_NoInvalidanElTokenNuevo_NiElOtroPerfil()
    {
        var emitidos = 0;
        using var handler = new Handler(_ => Task.FromResult(Json($$"""{"access_token":"t{{Interlocked.Increment(ref emitidos)}}","expires_in":3600}""")));
        var fabrica = new Fabrica(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var tokens = new DemoTokenProvider(fabrica, cache);
        var admin = await tokens.ObtenerAsync("Administrador", default);
        var viejo = await tokens.ObtenerAsync("Cajero", default);
        var renovados = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => tokens.RenovarAsync("Cajero", viejo!, default)));
        Assert.Single(renovados.Distinct());
        Assert.NotEqual(viejo, renovados[0]);
        Assert.Equal(admin, await tokens.ObtenerAsync("Administrador", default));
        await tokens.InvalidarAsync("Cajero", viejo!, default);
        Assert.Equal(renovados[0], await tokens.ObtenerAsync("Cajero", default));
        Assert.Equal(3, emitidos);
    }

    [Fact]
    public async Task FalloAlRenovar_Devuelve503_SinReenviarLaOperacion()
    {
        var emitidos = 0;
        var operaciones = 0;
        using var handler = new Handler(r =>
        {
            if (r.RequestUri!.AbsolutePath == "/dev-auth/token")
                return Task.FromResult(++emitidos == 1 ? Json("""{"access_token":"viejo","expires_in":3600}""") : new(HttpStatusCode.ServiceUnavailable));
            operaciones++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });
        var fabrica = new Fabrica(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var r = await Cliente(fabrica, new DemoTokenProvider(fabrica, cache)).DesactivarAsync("id", default);
        Assert.Equal(503, r.Status);
        Assert.Equal(1, operaciones);
        Assert.Equal(2, emitidos);
    }
}
