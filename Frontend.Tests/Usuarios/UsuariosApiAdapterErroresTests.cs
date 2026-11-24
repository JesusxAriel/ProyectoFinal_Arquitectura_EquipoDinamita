using System.Net;
using PuntoMarket.Frontend.Auth;
using PuntoMarket.Frontend.Tests.Auth;
using PuntoMarket.Frontend.Usuarios;

namespace PuntoMarket.Frontend.Tests.Usuarios;

public sealed class UsuariosApiAdapterErroresTests
{
    private static UsuariosApiAdapter Crear(HttpStatusCode estado, string json) =>
        new(new HttpClient(FakeHttpMessageHandler.Returning(estado, json)) { BaseAddress = new Uri("http://gateway/") });

    [Fact]
    public async Task Forbidden_ConMensajeDeLaApi_MuestraEseMensaje()
    {
        var adaptador = Crear(HttpStatusCode.Forbidden, """{"error":"Debe cambiar su contraseña antes de continuar."}""");

        var resultado = await adaptador.Listar();

        Assert.True(resultado.IsFail);
        Assert.Equal("Debe cambiar su contraseña antes de continuar.", resultado.Error);
    }

    [Fact]
    public async Task Forbidden_SinCuerpo_MuestraSinPermisos()
    {
        var adaptador = Crear(HttpStatusCode.Forbidden, "");

        var resultado = await adaptador.Listar();

        Assert.Equal(MensajesAuth.SinPermisos, resultado.Error);
    }
}
