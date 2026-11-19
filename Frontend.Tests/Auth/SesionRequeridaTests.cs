using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Tests.Auth;

public sealed class SesionRequeridaTests
{
    private readonly DefaultHttpContext _context = new() { Session = new FakeSession() };
    private bool _siguio;

    private async Task<HttpContext> Invocar(string path, LoginResponse? login)
    {
        var sesion = new SesionUsuario(
            new HttpContextAccessor { HttpContext = _context },
            TokenProvider.Instance,
            new FakeTimeProvider(DateTimeOffset.Parse("2026-11-19T10:00:00Z")));
        if (login is not null)
        {
            sesion.Iniciar(login);
        }

        _context.Request.Path = path;
        var middleware = new SesionRequeridaMiddleware(_ =>
        {
            _siguio = true;
            return Task.CompletedTask;
        });
        await middleware.InvokeAsync(_context, sesion);
        return _context;
    }

    private static LoginResponse LoginConCambio(bool debeCambiar) =>
        new("jwt-de-prueba", 1800, "admin.test", "Ana Prueba", "Administrador", debeCambiar);

    [Theory]
    [InlineData("/")]
    [InlineData("/Index")]
    [InlineData("/Proveedores")]
    public async Task SinSesion_RedirigeAlLogin(string path)
    {
        var context = await Invocar(path, null);

        Assert.Equal(RutasAuth.Login, context.Response.Headers.Location);
        Assert.False(_siguio);
    }

    [Theory]
    [InlineData("/Auth/Login")]
    [InlineData("/Auth/Logout")]
    [InlineData("/Error")]
    public async Task SinSesion_RutasPublicasContinuan(string path)
    {
        await Invocar(path, null);

        Assert.True(_siguio);
    }

    [Fact]
    public async Task ConSesion_ContinuaAlaPagina()
    {
        await Invocar("/Proveedores", LoginConCambio(false));

        Assert.True(_siguio);
    }

    [Fact]
    public async Task ConSesionYCambioPendiente_RedirigeAlCambioObligatorio()
    {
        var context = await Invocar("/Proveedores", LoginConCambio(true));

        Assert.Equal(RutasAuth.CambiarPasswordObligatorio, context.Response.Headers.Location);
        Assert.False(_siguio);
    }

    [Fact]
    public async Task ConSesionYCambioPendiente_LaPaginaDeCambioContinua()
    {
        await Invocar(RutasAuth.CambiarPasswordObligatorio, LoginConCambio(true));

        Assert.True(_siguio);
    }
}
