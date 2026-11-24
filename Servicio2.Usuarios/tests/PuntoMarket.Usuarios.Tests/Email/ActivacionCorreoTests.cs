using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Infrastructure.Activacion;
using PuntoMarket.Usuarios.Infrastructure.DependencyInjection;
using PuntoMarket.Usuarios.Infrastructure.Email;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Email;

public class ActivacionCorreoTests
{
    private static IConfiguration Configuracion(params (string Clave, string? Valor)[] valores) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(valores.ToDictionary(v => v.Clave, v => v.Valor))
            .Build();

    [Theory]
    [InlineData("http://localhost:5100", "abc", "http://localhost:5100/Auth/Activar?token=abc")]
    [InlineData("http://localhost:5100/", "abc", "http://localhost:5100/Auth/Activar?token=abc")]
    [InlineData("https://app.ejemplo.test/puntomarket/", "abc", "https://app.ejemplo.test/puntomarket/Auth/Activar?token=abc")]
    [InlineData("  https://app.ejemplo.test  ", "a-b_c", "https://app.ejemplo.test/Auth/Activar?token=a-b_c")]
    public void El_enlace_combina_la_url_base_con_la_ruta_de_activacion(string baseUrl, string token, string esperado)
    {
        Assert.Equal(esperado, SmtpEnviadorCorreo.ComponerEnlace(baseUrl, token));
    }

    [Fact]
    public void El_token_se_codifica_en_la_consulta()
    {
        var enlace = SmtpEnviadorCorreo.ComponerEnlace("http://localhost:5100", "a+b/c=d");

        Assert.Equal("http://localhost:5100/Auth/Activar?token=a%2Bb%2Fc%3Dd", enlace);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-es-una-url")]
    [InlineData("ftp://servidor.test")]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://usuario:clave@servidor.test")]
    [InlineData("http://servidor.test/?a=1")]
    [InlineData("http://servidor.test/#fragmento")]
    public void Una_url_base_invalida_no_produce_enlace(string baseUrl)
    {
        Assert.Null(SmtpEnviadorCorreo.ComponerEnlace(baseUrl, "abc"));
    }

    [Theory]
    [InlineData(0.2, "1 minuto")]
    [InlineData(1, "1 minuto")]
    [InlineData(10, "10 minutos")]
    [InlineData(60, "1 hora")]
    [InlineData(90, "90 minutos")]
    [InlineData(120, "2 horas")]
    public void La_vigencia_se_describe_en_espanol(double minutos, string esperado)
    {
        Assert.Equal(esperado, SmtpEnviadorCorreo.DescribirVigencia(TimeSpan.FromMinutes(minutos)));
    }

    [Fact]
    public async Task Con_la_url_de_activacion_invalida_el_envio_falla_con_mensaje_claro_sin_el_token()
    {
        var enviador = new SmtpEnviadorCorreo(
            new SmtpEnviadorCorreoOpciones
            {
                Host = "smtp.ejemplo.test", Port = 25, FromAddress = "no-responder@ejemplo.test"
            },
            new ActivacionOpciones { BaseUrl = "no-es-una-url" });

        var resultado = await enviador.EnviarActivacion("ana@ejemplo.test", "aperez", "token-secreto", TimeSpan.FromMinutes(60));

        Assert.True(resultado.IsFail);
        Assert.Equal(SmtpEnviadorCorreo.UrlDeActivacionNoConfigurada, resultado.Error);
        Assert.DoesNotContain("token-secreto", resultado.Error);
    }

    [Fact]
    public void Sin_configuracion_la_vigencia_es_de_60_minutos_y_la_url_queda_vacia()
    {
        var opciones = ActivacionOpciones.Desde(Configuracion());

        Assert.Equal(60, opciones.VigenciaMinutos);
        Assert.Equal(TimeSpan.FromMinutes(60), opciones.Vigencia);
        Assert.Equal("", opciones.BaseUrl);
    }

    [Fact]
    public void Lee_la_vigencia_y_la_url_configuradas()
    {
        var opciones = ActivacionOpciones.Desde(Configuracion(
            ("Activacion:VigenciaMinutos", "15"), ("Activacion:BaseUrl", "  http://localhost:5100  ")));

        Assert.Equal(15, opciones.VigenciaMinutos);
        Assert.Equal("http://localhost:5100", opciones.BaseUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("abc")]
    [InlineData("10081")]
    public void Una_vigencia_invalida_vuelve_al_valor_predeterminado(string valor)
    {
        var opciones = ActivacionOpciones.Desde(Configuracion(("Activacion:VigenciaMinutos", valor)));

        Assert.Equal(60, opciones.VigenciaMinutos);
    }

    [Fact]
    public void La_configuracion_de_activacion_se_resuelve_desde_la_inyeccion_de_dependencias()
    {
        var configuracion = Configuracion(
            ("ConnectionStrings:MySqlConnection", "Server=localhost;Database=x;User ID=x;Password=x"),
            ("Keycloak:BaseUrl", "http://keycloak:8080"),
            ("Keycloak:Realm", "puntomarket"),
            ("Keycloak:LoginClientId", "puntomarket-web"),
            ("Keycloak:AdminUserName", "admin"),
            ("Keycloak:AdminPassword", "x"),
            ("Activacion:VigenciaMinutos", "30"));
        var servicios = new ServiceCollection();
        servicios.AgregarInfraestructuraGestion(configuracion);
        using var proveedor = servicios.BuildServiceProvider();

        Assert.Equal(TimeSpan.FromMinutes(30), proveedor.GetRequiredService<ConfiguracionActivacion>().Vigencia);
        Assert.NotNull(proveedor.GetRequiredService<IGeneradorTokenActivacion>());
        Assert.NotNull(proveedor.GetRequiredService<IEnviadorCorreo>());
    }
}
