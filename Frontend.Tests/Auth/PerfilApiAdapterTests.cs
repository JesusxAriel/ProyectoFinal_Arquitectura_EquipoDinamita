using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Tests.Auth;

public sealed class PerfilApiAdapterTests
{
    private static readonly CambiarPasswordRequest Request = new("Actual#123", "Nueva#123", "Nueva#123");

    private static PerfilApiAdapter Crear(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://gateway.test/") });

    [Fact]
    public async Task CambiarPassword_EnviaPutConLosTresCamposSinAlterarlos()
    {
        string? body = null;
        string? contentType = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            contentType = request.Content.Headers.ContentType?.MediaType;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var entrada = new CambiarPasswordRequest(" Actual#123 ", " Nueva#123 ", " Nueva#123 ");

        var result = await Crear(handler).CambiarPassword(entrada);

        Assert.True(result.IsOk);
        var enviado = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, enviado.Method);
        Assert.Equal("http://gateway.test/api/usuarios/perfil/password", enviado.RequestUri!.ToString());
        Assert.Equal("application/json", contentType);
        using var json = JsonDocument.Parse(body!);
        Assert.Equal(3, json.RootElement.EnumerateObject().Count());
        Assert.Equal(entrada.Actual, json.RootElement.GetProperty("actual").GetString());
        Assert.Equal(entrada.Nueva, json.RootElement.GetProperty("nueva").GetString());
        Assert.Equal(entrada.Confirmacion, json.RootElement.GetProperty("confirmacion").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "")]
    [InlineData(HttpStatusCode.OK, "respuesta sin JSON")]
    [InlineData(HttpStatusCode.NoContent, "")]
    public async Task CambiarPassword_Exito_NoRequiereDeserializarRespuesta(HttpStatusCode status, string body)
    {
        var result = await Crear(FakeHttpMessageHandler.Returning(status, body)).CambiarPassword(Request);

        Assert.True(result.IsOk);
    }

    [Fact]
    public async Task CambiarPassword_400ConMensaje_DevuelveElMensajeDelBackend()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.BadRequest,
            """{"error":"La contraseña actual es incorrecta."}""");

        var result = await Crear(handler).CambiarPassword(Request);

        Assert.True(result.IsFail);
        Assert.Equal("La contraseña actual es incorrecta.", result.Error);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not JSON")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("""{"error":null}""")]
    [InlineData("""{"error":""}""")]
    [InlineData("""{"error":"   "}""")]
    [InlineData("""{"error":123}""")]
    [InlineData("""{"error":true}""")]
    [InlineData("""{"error":{}}""")]
    [InlineData("""{"error":[]}""")]
    public async Task CambiarPassword_400SinMensajeValido_DevuelveMensajeGenerico(string body)
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.BadRequest, body);

        var result = await Crear(handler).CambiarPassword(Request);

        Assert.True(result.IsFail);
        Assert.Equal("No se pudo cambiar la contraseña. Revise los datos ingresados.", result.Error);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "No se pudo autorizar el cambio de contraseña. Verifique su sesión.")]
    [InlineData(HttpStatusCode.Forbidden, "No tiene permisos para realizar esta acción.")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "El servicio no está disponible. Intente nuevamente en unos minutos.")]
    [InlineData(HttpStatusCode.InternalServerError, "Ocurrió un error inesperado. Intente nuevamente.")]
    public async Task CambiarPassword_Error_TraduceEstadoConCualquierCuerpo(HttpStatusCode status, string esperado)
    {
        foreach (var body in new[] { "", "not JSON", "{\"error\":\"Internal server error\"}" })
        {
            var handler = FakeHttpMessageHandler.Returning(status, body);

            var result = await Crear(handler).CambiarPassword(Request);

            Assert.True(result.IsFail);
            Assert.Equal(esperado, result.Error);
            Assert.Single(handler.Requests);
        }
    }

    [Fact]
    public async Task CambiarPassword_SinConexion_DevuelveFailSinReintentar()
    {
        var handler = FakeHttpMessageHandler.Throwing();

        var result = await Crear(handler).CambiarPassword(Request);

        Assert.True(result.IsFail);
        Assert.Equal(MensajesAuth.SinConexion, result.Error);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task CambiarPassword_Timeout_DevuelveFailSinReintentar()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new TaskCanceledException("timeout"));

        var result = await Crear(handler).CambiarPassword(Request);

        Assert.True(result.IsFail);
        Assert.Equal("El servidor tardó demasiado en responder. Intente nuevamente.", result.Error);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("jwt-sesion-prueba")]
    [InlineData(null)]
    public async Task RegistroExistente_UsaGatewayYTokenDeSesion(string? token)
    {
        var context = new DefaultHttpContext { Session = new FakeSession() };
        if (token is not null)
            TokenProvider.Instance.Guardar(context.Session, token);
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.NoContent);
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GatewayApi:BaseUrl"] = "http://gateway.test/"
        }).Build();
        services.AddAutenticacionPorSesion(configuration);
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = context });
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => handler));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<IPerfilApiAdapter>().CambiarPassword(Request);

        Assert.True(result.IsOk);
        var enviado = Assert.Single(handler.Requests);
        Assert.Equal("http://gateway.test/api/usuarios/perfil/password", enviado.RequestUri!.ToString());
        Assert.Equal(token, enviado.Headers.Authorization?.Parameter);
        Assert.Equal(token is null ? null : "Bearer", enviado.Headers.Authorization?.Scheme);
        Assert.Equal(token, TokenProvider.Instance.Obtener(context.Session));
    }
}
