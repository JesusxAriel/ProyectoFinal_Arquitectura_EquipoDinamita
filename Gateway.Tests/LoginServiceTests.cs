using System.Net;
using Microsoft.Extensions.Options;
using PuntoMarket.Gateway.Auth;

namespace PuntoMarket.Gateway.Tests;

public sealed class LoginServiceTests
{
    private const string TokenJson = """{"access_token":"jwt-de-prueba","expires_in":1800}""";
    private const string UsuarioJson = """
        {"id":1,"username":"admin.test","nombreCompleto":"Ana Prueba","correo":"a@b.com",
         "rol":"Administrador","activo":true,"debeCambiarPassword":true}
        """;
    private const string UsuarioInactivoJson = """
        {"id":1,"username":"admin.test","nombreCompleto":"Ana Prueba","correo":"a@b.com",
         "rol":"Cajero","activo":false,"debeCambiarPassword":false}
        """;

    private static readonly LoginRequest ValidRequest = new("admin.test", "Clave#123");

    private static LoginService CreateService(
        HttpMessageHandler keycloakHandler, HttpMessageHandler usuariosHandler)
    {
        var keycloakOptions = Options.Create(new KeycloakOptions
        {
            TokenEndpoint = "http://keycloak.test/token",
            LogoutEndpoint = "http://keycloak.test/logout",
            ClientId = "cliente-de-prueba"
        });
        var usuariosOptions = Options.Create(new UsuariosOptions
        {
            BaseUrl = "http://usuarios.test/",
            CurrentUserPath = "api/usuarios/yo"
        });

        var keycloak = new KeycloakClient(new HttpClient(keycloakHandler), keycloakOptions);
        var usuarios = new UsuariosClient(
            new HttpClient(usuariosHandler) { BaseAddress = new Uri(usuariosOptions.Value.BaseUrl) },
            usuariosOptions);
        return new LoginService(keycloak, usuarios);
    }

    [Fact]
    public async Task CredencialesValidas_DevuelveLoginResponseCompleto()
    {
        var service = CreateService(
            FakeHttpMessageHandler.Returning(HttpStatusCode.OK, TokenJson),
            FakeHttpMessageHandler.Returning(HttpStatusCode.OK, UsuarioJson));

        var result = await service.Login(ValidRequest, CancellationToken.None);

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.Equal(
            new LoginResponse("jwt-de-prueba", 1800, "admin.test", "Ana Prueba", "Administrador", true),
            result.Response);
    }

    [Fact]
    public async Task CredencialesValidas_PideTokenConPasswordGrantYEnviaElTokenAUsuarios()
    {
        var keycloak = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, TokenJson);
        var usuarios = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, UsuarioJson);

        await CreateService(keycloak, usuarios).Login(ValidRequest, CancellationToken.None);

        var tokenRequest = Assert.Single(keycloak.Requests);
        var form = await tokenRequest.Content!.ReadAsStringAsync();
        Assert.Contains("grant_type=password", form);
        Assert.Contains("client_id=cliente-de-prueba", form);
        var userRequest = Assert.Single(usuarios.Requests);
        Assert.Equal("http://usuarios.test/api/usuarios/yo", userRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", userRequest.Headers.Authorization!.Scheme);
        Assert.Equal("jwt-de-prueba", userRequest.Headers.Authorization.Parameter);
    }

    [Theory]
    [InlineData(null, "Clave#123")]
    [InlineData("  ", "Clave#123")]
    [InlineData("admin.test", "")]
    [InlineData("admin.test", null)]
    public async Task CredencialesIncompletas_DevuelveSolicitudInvalidaSinLlamarAKeycloak(
        string? username, string? password)
    {
        var keycloak = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, TokenJson);
        var service = CreateService(keycloak, FakeHttpMessageHandler.Returning(HttpStatusCode.OK, UsuarioJson));

        var result = await service.Login(new LoginRequest(username, password), CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidRequest, result.Status);
        Assert.Equal(AuthMessages.CredentialsRequired, result.Error);
        Assert.Empty(keycloak.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task KeycloakRechazaLasCredenciales_DevuelveCredencialesInvalidas(HttpStatusCode status)
    {
        var usuarios = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, UsuarioJson);
        var service = CreateService(
            FakeHttpMessageHandler.Returning(status, """{"error":"invalid_grant"}"""), usuarios);

        var result = await service.Login(ValidRequest, CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Equal(AuthMessages.InvalidCredentials, result.Error);
        Assert.Empty(usuarios.Requests);
    }

    [Fact]
    public async Task KeycloakNoResponde_DevuelveServicioNoDisponible()
    {
        var service = CreateService(
            FakeHttpMessageHandler.Throwing(),
            FakeHttpMessageHandler.Returning(HttpStatusCode.OK, UsuarioJson));

        var result = await service.Login(ValidRequest, CancellationToken.None);

        Assert.Equal(LoginStatus.Unavailable, result.Status);
        Assert.Equal(AuthMessages.AuthenticationUnavailable, result.Error);
    }

    [Fact]
    public async Task UsuariosNoResponde_DevuelveServicioNoDisponible()
    {
        var service = CreateService(
            FakeHttpMessageHandler.Returning(HttpStatusCode.OK, TokenJson),
            FakeHttpMessageHandler.Throwing());

        var result = await service.Login(ValidRequest, CancellationToken.None);

        Assert.Equal(LoginStatus.Unavailable, result.Status);
        Assert.Equal(AuthMessages.UsersUnavailable, result.Error);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task UsuariosResponderConErrorDeServidor_DevuelveServicioNoDisponible(HttpStatusCode status)
    {
        var service = CreateService(
            FakeHttpMessageHandler.Returning(HttpStatusCode.OK, TokenJson),
            FakeHttpMessageHandler.Returning(status));

        var result = await service.Login(ValidRequest, CancellationToken.None);

        Assert.Equal(LoginStatus.Unavailable, result.Status);
        Assert.Equal(AuthMessages.UsersUnavailable, result.Error);
    }

    [Fact]
    public async Task UsuarioNoRegistradoEnElSistema_DevuelveUsuarioNoHabilitado()
    {
        var service = CreateService(
            FakeHttpMessageHandler.Returning(HttpStatusCode.OK, TokenJson),
            FakeHttpMessageHandler.Returning(HttpStatusCode.NotFound));

        var result = await service.Login(ValidRequest, CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Equal(AuthMessages.UserNotEnabled, result.Error);
    }

    [Fact]
    public async Task UsuarioInactivo_DevuelveUsuarioNoHabilitado()
    {
        var service = CreateService(
            FakeHttpMessageHandler.Returning(HttpStatusCode.OK, TokenJson),
            FakeHttpMessageHandler.Returning(HttpStatusCode.OK, UsuarioInactivoJson));

        var result = await service.Login(ValidRequest, CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Equal(AuthMessages.UserNotEnabled, result.Error);
    }

    [Fact]
    public async Task RevocarRefreshToken_EnviaClienteYTokenAKeycloak()
    {
        var keycloakHandler = FakeHttpMessageHandler.Returning(HttpStatusCode.NoContent);
        var client = new KeycloakClient(
            new HttpClient(keycloakHandler),
            Options.Create(new KeycloakOptions { LogoutEndpoint = "http://keycloak.test/logout", ClientId = "cliente-de-prueba" }));

        await client.RevokeRefreshToken("refresh-de-prueba", CancellationToken.None);

        var request = Assert.Single(keycloakHandler.Requests);
        Assert.Equal("http://keycloak.test/logout", request.RequestUri!.ToString());
        var form = await request.Content!.ReadAsStringAsync();
        Assert.Contains("refresh_token=refresh-de-prueba", form);
        Assert.Contains("client_id=cliente-de-prueba", form);
    }
}
