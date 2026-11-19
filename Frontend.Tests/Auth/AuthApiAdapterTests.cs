using System.Net;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Tests.Auth;

public sealed class AuthApiAdapterTests
{
    private const string LoginJson = """
        {"accessToken":"jwt-de-prueba","expiresIn":1800,"username":"admin.test",
         "nombreCompleto":"Ana Prueba","rol":"Administrador","debeCambiarPassword":true}
        """;

    private static readonly LoginRequest Request = new("admin.test", "Clave#123");

    private static AuthApiAdapter CreateAdapter(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://gateway.test/") });

    [Fact]
    public async Task Login_RespuestaCorrecta_DevuelveElLoginResponse()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, LoginJson);

        var result = await CreateAdapter(handler).Login(Request);

        Assert.True(result.IsOk);
        Assert.Equal("jwt-de-prueba", result.Value.AccessToken);
        Assert.Equal("Administrador", result.Value.Rol);
        Assert.True(result.Value.DebeCambiarPassword);
        Assert.Equal("http://gateway.test/api/auth/login", handler.Requests.Single().RequestUri!.ToString());
    }

    [Fact]
    public async Task Login_401ConMensaje_DevuelveElMensajeDelGateway()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.Unauthorized, """{"error":"Usuario o contraseña incorrectos."}""");

        var result = await CreateAdapter(handler).Login(Request);

        Assert.True(result.IsFail);
        Assert.Equal(MensajesAuth.CredencialesIncorrectas, result.Error);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, MensajesAuth.CredencialesIncorrectas)]
    [InlineData(HttpStatusCode.BadRequest, MensajesAuth.SolicitudInvalida)]
    [InlineData(HttpStatusCode.Forbidden, MensajesAuth.SinPermisos)]
    [InlineData(HttpStatusCode.ServiceUnavailable, MensajesAuth.ServicioNoDisponible)]
    [InlineData(HttpStatusCode.BadGateway, MensajesAuth.ErrorInesperado)]
    public async Task Login_ErrorSinCuerpo_TraduceElCodigoAMensajeEnEspanol(HttpStatusCode status, string esperado)
    {
        var result = await CreateAdapter(FakeHttpMessageHandler.Returning(status)).Login(Request);

        Assert.True(result.IsFail);
        Assert.Equal(esperado, result.Error);
    }

    [Fact]
    public async Task Login_503ConMensaje_DevuelveElMensajeDelGateway()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable, """{"error":"El servicio de usuarios no está disponible."}""");

        var result = await CreateAdapter(handler).Login(Request);

        Assert.Equal("El servicio de usuarios no está disponible.", result.Error);
    }

    [Fact]
    public async Task Login_SinConexion_NoLanzaYDevuelveFail()
    {
        var result = await CreateAdapter(FakeHttpMessageHandler.Throwing()).Login(Request);

        Assert.True(result.IsFail);
        Assert.Equal(MensajesAuth.SinConexion, result.Error);
    }

    [Fact]
    public async Task Login_CuerpoNoJson_NoLanzaYDevuelveFail()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "no es json");

        var result = await CreateAdapter(handler).Login(Request);

        Assert.True(result.IsFail);
    }

    [Fact]
    public async Task Logout_204_DevuelveOk()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.NoContent);

        var result = await CreateAdapter(handler).Logout();

        Assert.True(result.IsOk);
        Assert.Equal("http://gateway.test/api/auth/logout", handler.Requests.Single().RequestUri!.ToString());
    }

    [Fact]
    public async Task Logout_SinConexion_NoLanzaYDevuelveFail()
    {
        var result = await CreateAdapter(FakeHttpMessageHandler.Throwing()).Logout();

        Assert.True(result.IsFail);
        Assert.Equal(MensajesAuth.SinConexion, result.Error);
    }
}
