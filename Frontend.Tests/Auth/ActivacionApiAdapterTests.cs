using System.Net;
using System.Text.Json;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Tests.Auth;

public class ActivacionApiAdapterTests
{
    private const string MensajePolitica = "La contraseña debe tener al menos 8 caracteres.";
    private const string MensajeServicio = "No se pudo completar la activación. Intente nuevamente más tarde.";

    private static ActivacionApiAdapter Adaptador(FakeHttpMessageHandler manejador) =>
        new(new HttpClient(manejador) { BaseAddress = new Uri("http://gateway.test/") });

    [Fact]
    public async Task Una_activacion_exitosa_envia_el_cuerpo_esperado_sin_credenciales()
    {
        var manejador = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, """{"activada":true}""");

        var resultado = await Adaptador(manejador).Activar("tok-1", "NuevaClave#2026", "NuevaClave#2026");

        Assert.True(resultado.IsOk);
        var peticion = Assert.Single(manejador.Requests);
        Assert.Equal(HttpMethod.Post, peticion.Method);
        Assert.Equal("http://gateway.test/api/activacion", peticion.RequestUri!.ToString());
        Assert.Null(peticion.Headers.Authorization);
        using var cuerpo = JsonDocument.Parse(await peticion.Content!.ReadAsStringAsync());
        Assert.Equal("tok-1", cuerpo.RootElement.GetProperty("token").GetString());
        Assert.Equal("NuevaClave#2026", cuerpo.RootElement.GetProperty("nuevaContrasena").GetString());
        Assert.Equal("NuevaClave#2026", cuerpo.RootElement.GetProperty("confirmarContrasena").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, MensajePolitica)]
    [InlineData(HttpStatusCode.BadRequest, MensajesActivacion.EnlaceInvalido)]
    [InlineData(HttpStatusCode.ServiceUnavailable, MensajeServicio)]
    public async Task Los_mensajes_del_backend_se_devuelven_tal_cual(HttpStatusCode estado, string mensaje)
    {
        var json = JsonSerializer.Serialize(new { error = mensaje });
        var manejador = FakeHttpMessageHandler.Returning(estado, json);

        var resultado = await Adaptador(manejador).Activar("tok-1", "a", "a");

        Assert.True(resultado.IsFail);
        Assert.Equal(mensaje, resultado.Error);
    }

    [Fact]
    public async Task Un_503_no_se_reintenta()
    {
        var manejador = FakeHttpMessageHandler.Returning(
            HttpStatusCode.ServiceUnavailable, JsonSerializer.Serialize(new { error = MensajeServicio }));

        await Adaptador(manejador).Activar("tok-1", "a", "a");

        Assert.Single(manejador.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "", MensajesAuth.SolicitudInvalida)]
    [InlineData(HttpStatusCode.BadRequest, "no es json", MensajesAuth.SolicitudInvalida)]
    [InlineData(HttpStatusCode.ServiceUnavailable, "", MensajesAuth.ServicioNoDisponible)]
    [InlineData(HttpStatusCode.InternalServerError, "", MensajesAuth.ErrorInesperado)]
    [InlineData(HttpStatusCode.Unauthorized, """{"error":"No debe mostrarse"}""", MensajesAuth.ErrorInesperado)]
    [InlineData(HttpStatusCode.Redirect, "", MensajesAuth.ErrorInesperado)]
    public async Task Sin_mensaje_util_del_backend_usa_un_texto_generico_en_espanol(
        HttpStatusCode estado, string cuerpo, string esperado)
    {
        var resultado = await Adaptador(FakeHttpMessageHandler.Returning(estado, cuerpo)).Activar("tok-1", "a", "a");

        Assert.True(resultado.IsFail);
        Assert.Equal(esperado, resultado.Error);
    }

    [Theory]
    [InlineData("""{"activada":false}""")]
    [InlineData("""{}""")]
    [InlineData("""[]""")]
    [InlineData("no es json")]
    [InlineData("")]
    public async Task Un_200_sin_activada_en_verdadero_nunca_se_toma_por_exito(string cuerpo)
    {
        var resultado = await Adaptador(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, cuerpo)).Activar("tok-1", "a", "a");

        Assert.True(resultado.IsFail);
        Assert.Equal(MensajesAuth.ErrorInesperado, resultado.Error);
    }

    [Fact]
    public async Task Un_fallo_de_conexion_devuelve_el_mensaje_de_sin_conexion()
    {
        var resultado = await Adaptador(FakeHttpMessageHandler.Throwing()).Activar("tok-1", "a", "a");

        Assert.True(resultado.IsFail);
        Assert.Equal(MensajesAuth.SinConexion, resultado.Error);
    }

    [Fact]
    public async Task Un_tiempo_de_espera_agotado_devuelve_el_mensaje_de_sin_conexion()
    {
        var manejador = new FakeHttpMessageHandler(_ => throw new TaskCanceledException("tiempo agotado"));

        var resultado = await Adaptador(manejador).Activar("tok-1", "a", "a");

        Assert.True(resultado.IsFail);
        Assert.Equal(MensajesAuth.SinConexion, resultado.Error);
    }
}
