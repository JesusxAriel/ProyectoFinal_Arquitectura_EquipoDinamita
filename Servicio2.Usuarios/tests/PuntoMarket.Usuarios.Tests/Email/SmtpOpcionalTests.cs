using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Infrastructure.DependencyInjection;
using PuntoMarket.Usuarios.Infrastructure.Email;

namespace PuntoMarket.Usuarios.Tests.Email;

public sealed class SmtpOpcionalTests
{
    private static ServiceProvider Construir(Dictionary<string, string?> valores)
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var servicios = new ServiceCollection();
        servicios.AgregarInfraestructuraGestion(configuracion);
        return servicios.BuildServiceProvider();
    }

    private static Dictionary<string, string?> SmtpVacio() => new()
    {
        ["ConnectionStrings:MySqlConnection"] = "Server=localhost;Database=x;User ID=x;Password=x",
        ["Keycloak:BaseUrl"] = "http://keycloak:8080",
        ["Keycloak:Realm"] = "puntomarket",
        ["Keycloak:LoginClientId"] = "puntomarket-web",
        ["Keycloak:AdminUserName"] = "admin",
        ["Keycloak:AdminPassword"] = "x",
        ["Smtp:Host"] = "",
        ["Smtp:Port"] = "",
        ["Smtp:EnableSsl"] = "",
        ["Smtp:FromAddress"] = "",
        ["Smtp:FromName"] = "",
        ["Smtp:UserName"] = "",
        ["Smtp:Password"] = ""
    };

    [Fact]
    public void ConSmtpVacio_SeResuelveElEnviadorSinLanzarExcepcion()
    {
        using var proveedor = Construir(SmtpVacio());

        var enviador = proveedor.GetRequiredService<IEnviadorCorreo>();

        Assert.NotNull(enviador);
    }

    [Fact]
    public void ConSmtpVacio_LasOpcionesQuedanSinConfigurar()
    {
        using var proveedor = Construir(SmtpVacio());

        var opciones = proveedor.GetRequiredService<SmtpEnviadorCorreoOpciones>();

        Assert.Equal("", opciones.Host);
        Assert.Equal(0, opciones.Port);
    }

    [Fact]
    public void SinConfiguracionDelFrontend_ElEnlaceDeLoginUsaLaDireccionPorDefecto()
    {
        using var proveedor = Construir(SmtpVacio());

        var opciones = proveedor.GetRequiredService<SmtpEnviadorCorreoOpciones>();

        Assert.Equal("http://localhost:5100", opciones.FrontendBaseUrl);
        Assert.Equal("http://localhost:5100/Auth/Login", opciones.UrlLogin);
    }

    [Theory]
    [InlineData("https://puntomarket.ejemplo.test", "https://puntomarket.ejemplo.test/Auth/Login")]
    [InlineData("https://puntomarket.ejemplo.test/", "https://puntomarket.ejemplo.test/Auth/Login")]
    [InlineData("  http://localhost:5100  ", "http://localhost:5100/Auth/Login")]
    public void ConFrontendBaseUrl_ElEnlaceDeLoginSeArmaSinBarrasDuplicadas(string baseUrl, string esperado)
    {
        var valores = SmtpVacio();
        valores["Frontend:BaseUrl"] = baseUrl;
        using var proveedor = Construir(valores);

        var opciones = proveedor.GetRequiredService<SmtpEnviadorCorreoOpciones>();

        Assert.Equal(esperado, opciones.UrlLogin);
    }

    [Fact]
    public void ConSmtpCompleto_LeeLosValoresConfigurados()
    {
        var valores = SmtpVacio();
        valores["Smtp:Host"] = "smtp.ejemplo.test";
        valores["Smtp:Port"] = "2525";
        valores["Smtp:EnableSsl"] = "false";
        valores["Smtp:FromAddress"] = "no-responder@ejemplo.test";
        using var proveedor = Construir(valores);

        var opciones = proveedor.GetRequiredService<SmtpEnviadorCorreoOpciones>();

        Assert.Equal("smtp.ejemplo.test", opciones.Host);
        Assert.Equal(2525, opciones.Port);
        Assert.False(opciones.EnableSsl);
        Assert.Equal("no-responder@ejemplo.test", opciones.FromAddress);
    }

    [Theory]
    [InlineData("", "", "", "")]
    [InlineData("smtp.ejemplo.test", "", "no-responder@ejemplo.test", "")]
    [InlineData("smtp.ejemplo.test", "0", "no-responder@ejemplo.test", "")]
    public async Task ConConfiguracionIncompleta_EnviarDevuelveResultFallidoEnEspanol(
        string host, string puerto, string desde, string _)
    {
        var valores = SmtpVacio();
        valores["Smtp:Host"] = host;
        valores["Smtp:Port"] = puerto;
        valores["Smtp:FromAddress"] = desde;
        using var proveedor = Construir(valores);
        var enviador = proveedor.GetRequiredService<IEnviadorCorreo>();

        var resultado = await enviador.EnviarCredenciales("ana@ejemplo.test", "aperez", "Clave#Temporal1");

        Assert.True(resultado.IsFail);
        Assert.Contains("SMTP", resultado.Error);
        Assert.DoesNotContain("Clave#Temporal1", resultado.Error);
    }

    [Fact]
    public async Task ConUsuarioSinPassword_EnviarDevuelveResultFallido()
    {
        var enviador = new SmtpEnviadorCorreo(new SmtpEnviadorCorreoOpciones
        {
            Host = "smtp.ejemplo.test", Port = 25, FromAddress = "no-responder@ejemplo.test", UserName = "u"
        });

        var resultado = await enviador.EnviarCredenciales("ana@ejemplo.test", "aperez", "Clave#Temporal1");

        Assert.True(resultado.IsFail);
        Assert.Contains("UserName y Password", resultado.Error);
    }
}
