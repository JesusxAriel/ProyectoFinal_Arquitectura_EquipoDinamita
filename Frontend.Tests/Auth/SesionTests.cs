using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Tests.Auth;

public sealed class SesionTests
{
    private static readonly LoginResponse Login =
        new("jwt-de-prueba", 1800, "admin.test", "Ana Prueba", "Administrador", true);

    private readonly FakeTimeProvider _reloj = new(DateTimeOffset.Parse("2026-11-19T10:00:00Z"));
    private readonly DefaultHttpContext _context = new() { Session = new FakeSession() };

    private SesionUsuario CreateSesion() =>
        new(new HttpContextAccessor { HttpContext = _context }, TokenProvider.Instance, _reloj);

    [Fact]
    public void TokenProvider_EsUnaUnicaInstancia()
    {
        Assert.Same(TokenProvider.Instance, TokenProvider.Instance);
    }

    [Fact]
    public void Iniciar_GuardaLosDatosYElToken()
    {
        var sesion = CreateSesion();

        sesion.Iniciar(Login);

        Assert.Equal("Ana Prueba", sesion.Actual!.NombreCompleto);
        Assert.True(sesion.Actual.DebeCambiarPassword);
        Assert.Equal("jwt-de-prueba", TokenProvider.Instance.Obtener(_context.Session));
    }

    [Fact]
    public void Iniciar_LimpiaLaSesionAnterior()
    {
        var sesion = CreateSesion();
        _context.Session.SetString("dato.anterior", "x");

        sesion.Iniciar(Login);

        Assert.Null(_context.Session.GetString("dato.anterior"));
    }

    [Fact]
    public void Cerrar_EliminaDatosYToken()
    {
        var sesion = CreateSesion();
        sesion.Iniciar(Login);

        sesion.Cerrar();

        Assert.Null(sesion.Actual);
        Assert.Null(TokenProvider.Instance.Obtener(_context.Session));
    }

    [Fact]
    public void TokenVencido_NoHaySesionActual()
    {
        var sesion = CreateSesion();
        sesion.Iniciar(Login);

        _reloj.Advance(TimeSpan.FromSeconds(1801));

        Assert.Null(sesion.Actual);
    }

    [Fact]
    public async Task BearerTokenHandler_AgregaElTokenDeLaSesion()
    {
        CreateSesion().Iniciar(Login);
        var inner = FakeHttpMessageHandler.Returning(HttpStatusCode.OK);
        var handler = new BearerTokenHandler(new HttpContextAccessor { HttpContext = _context }, TokenProvider.Instance)
        {
            InnerHandler = inner
        };

        await new HttpClient(handler).GetAsync("http://gateway.test/api/proveedores");

        var authorization = inner.Requests.Single().Headers.Authorization!;
        Assert.Equal("Bearer", authorization.Scheme);
        Assert.Equal("jwt-de-prueba", authorization.Parameter);
    }

    [Fact]
    public async Task BearerTokenHandler_SinSesionNoAgregaEncabezado()
    {
        var inner = FakeHttpMessageHandler.Returning(HttpStatusCode.OK);
        var handler = new BearerTokenHandler(new HttpContextAccessor { HttpContext = _context }, TokenProvider.Instance)
        {
            InnerHandler = inner
        };

        await new HttpClient(handler).GetAsync("http://gateway.test/api/proveedores");

        Assert.Null(inner.Requests.Single().Headers.Authorization);
    }

    [Theory]
    [InlineData(null, "x", MensajesAuth.UsuarioRequerido)]
    [InlineData("  ", "x", MensajesAuth.UsuarioRequerido)]
    [InlineData("admin", "", MensajesAuth.PasswordRequerida)]
    [InlineData("admin", null, MensajesAuth.PasswordRequerida)]
    public void Validador_CamposVacios_DevuelveFailConMensaje(string? usuario, string? password, string mensaje)
    {
        var result = ValidadorCredenciales.Validar(usuario, password);

        Assert.True(result.IsFail);
        Assert.Equal(mensaje, result.Error);
    }

    [Fact]
    public void Validador_CamposCompletos_DevuelveOk()
    {
        Assert.True(ValidadorCredenciales.Validar("admin", "x").IsOk);
    }
}
