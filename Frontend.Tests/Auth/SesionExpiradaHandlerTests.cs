using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Tests.Auth;

public sealed class SesionExpiradaHandlerTests
{
    private readonly SesionUsuario _sesion;

    public SesionExpiradaHandlerTests()
    {
        var contexto = new DefaultHttpContext { Session = new FakeSession() };
        _sesion = new SesionUsuario(
            new HttpContextAccessor { HttpContext = contexto },
            TokenProvider.Instance,
            new FakeTimeProvider(DateTimeOffset.Parse("2026-11-19T10:00:00Z")));
        _sesion.Iniciar(new LoginResponse("jwt-de-prueba", 1800, "admin.test", "Ana Prueba", "Administrador", false));
    }

    private async Task Enviar(HttpStatusCode estado, string ruta, bool conToken)
    {
        var handler = new SesionExpiradaHandler(_sesion)
        {
            InnerHandler = FakeHttpMessageHandler.Returning(estado)
        };
        using var cliente = new HttpClient(handler) { BaseAddress = new Uri("http://gateway/") };
        using var peticion = new HttpRequestMessage(HttpMethod.Get, ruta);
        if (conToken)
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "jwt-de-prueba");
        }

        using var _ = await cliente.SendAsync(peticion);
    }

    [Fact]
    public async Task Respuesta401ConToken_LimpiaLaSesion()
    {
        await Enviar(HttpStatusCode.Unauthorized, "api/usuarios", conToken: true);

        Assert.Null(_sesion.Actual);
    }

    [Fact]
    public async Task Respuesta200_NoTocaLaSesion()
    {
        await Enviar(HttpStatusCode.OK, "api/usuarios", conToken: true);

        Assert.NotNull(_sesion.Actual);
    }

    [Fact]
    public async Task Respuesta401DelLogin_NoTocaLaSesion()
    {
        await Enviar(HttpStatusCode.Unauthorized, AuthApiAdapter.LoginRoute, conToken: true);

        Assert.NotNull(_sesion.Actual);
    }

    [Fact]
    public async Task Respuesta401SinToken_NoTocaLaSesion()
    {
        await Enviar(HttpStatusCode.Unauthorized, "api/usuarios", conToken: false);

        Assert.NotNull(_sesion.Actual);
    }
}
