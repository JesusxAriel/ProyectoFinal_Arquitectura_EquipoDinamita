using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Proveedores.Infrastructure.Integrations;

namespace Proveedores.Tests;

/// <summary>El adaptador HTTP se prueba con un HttpMessageHandler falso: no hay API de Productos real.</summary>
public class ProductoApiAdapterTests
{
    private sealed class HandlerFalso(Func<HttpRequestMessage, HttpResponseMessage> respuesta) : HttpMessageHandler
    {
        public HttpRequestMessage? UltimaSolicitud { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            UltimaSolicitud = request;
            return Task.FromResult(respuesta(request));
        }
    }

    private static ProductoApiAdapter Crear(HandlerFalso handler, bool habilitado = true, bool conBaseAddress = true)
    {
        var http = new HttpClient(handler);
        if (conBaseAddress) http.BaseAddress = new Uri("http://productos.test/");
        var opciones = Options.Create(new ProductosApiSettings { Enabled = habilitado, BaseUrl = "http://productos.test/" });
        return new ProductoApiAdapter(http, opciones, NullLogger<ProductoApiAdapter>.Instance);
    }

    private static HttpResponseMessage Json(HttpStatusCode codigo, string json) =>
        new(codigo) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

    [Fact]
    public async Task ConProductosActivos_DevuelveTrue_YUsaLaRutaConfigurada()
    {
        var handler = new HandlerFalso(_ => Json(HttpStatusCode.OK, "{\"cantidad\":3}"));
        var r = await Crear(handler).TieneProductosActivosAsync("abc123", default);

        Assert.True(r.IsSuccess);
        Assert.True(r.Value);
        Assert.Equal("http://productos.test/api/productos/proveedor/abc123/activos/conteo", handler.UltimaSolicitud!.RequestUri!.ToString());
    }

    [Fact]
    public async Task SinProductos_DevuelveFalseReal()
    {
        var r = await Crear(new HandlerFalso(_ => Json(HttpStatusCode.OK, "{\"cantidad\":0}"))).TieneProductosActivosAsync("x", default);
        Assert.True(r.IsSuccess);
        Assert.False(r.Value);
    }

    [Fact]
    public async Task NoConfigurado_DevuelveFailure_NuncaFalseSimulado()
    {
        var handler = new HandlerFalso(_ => throw new InvalidOperationException("no debe llamarse"));
        var r = await Crear(handler, habilitado: false).TieneProductosActivosAsync("x", default);

        Assert.True(r.IsFailure);
        Assert.Equal(ProductoApiAdapter.NoConfigurada, r.Error);
        Assert.Null(handler.UltimaSolicitud);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task RespuestaNoExitosa_DevuelveFailureNoDisponible(HttpStatusCode codigo)
    {
        var r = await Crear(new HandlerFalso(_ => new HttpResponseMessage(codigo))).TieneProductosActivosAsync("x", default);
        Assert.Equal(ProductoApiAdapter.NoDisponible, r.Error);
    }

    [Fact]
    public async Task ErrorDeRed_DevuelveFailure()
    {
        var r = await Crear(new HandlerFalso(_ => throw new HttpRequestException("sin red"))).TieneProductosActivosAsync("x", default);
        Assert.Equal(ProductoApiAdapter.NoDisponible, r.Error);
    }

    [Fact]
    public async Task CuerpoInvalido_DevuelveFailureRespuestaInvalida()
    {
        var r = await Crear(new HandlerFalso(_ => Json(HttpStatusCode.OK, "esto no es json"))).TieneProductosActivosAsync("x", default);
        Assert.Equal(ProductoApiAdapter.RespuestaInvalida, r.Error);
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"cantidad\":null}")]
    [InlineData("{\"cantidad\":-1}")]
    [InlineData("{\"cantidad\":1.5}")]
    [InlineData("{\"cantidad\":\"3\"}")]
    [InlineData("{\"cantidad\":2147483648}")]
    public async Task ConteoAusenteOInvalido_NoSeInterpretaComoCero(string json)
    {
        var r = await Crear(new HandlerFalso(_ => Json(HttpStatusCode.OK, json)))
            .TieneProductosActivosAsync("x", default);
        Assert.True(r.IsFailure);
        Assert.Equal(ProductoApiAdapter.RespuestaInvalida, r.Error);
    }
}
