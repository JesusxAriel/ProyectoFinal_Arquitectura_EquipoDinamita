using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Usuarios.Api.Contracts;
using PuntoMarket.Usuarios.Api.Contracts.Perfil;
using PuntoMarket.Usuarios.Api.Controllers;
using PuntoMarket.Usuarios.Application.UseCases.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.VerificarPasswordActual;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Infrastructure.Identity;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class VerificarPasswordActualTests
{
    private const string Username = "aperez";

    private static (VerificarPasswordActualCasoDeUso Caso, GestorDeIdentidadFalso Identidad) Preparar()
    {
        var identidad = new GestorDeIdentidadFalso();
        return (new VerificarPasswordActualCasoDeUso(identidad, new UsuarioActualStub(Username)), identidad);
    }

    [Fact]
    public async Task Una_contrasena_correcta_responde_valida()
    {
        var (caso, identidad) = Preparar();

        var resultado = await caso.EjecutarAsync(new("Actual#123"));

        Assert.True(resultado.IsOk);
        Assert.True(resultado.Value.Valida);
        Assert.Equal((Username, "Actual#123"), Assert.Single(identidad.Verificaciones));
    }

    [Fact]
    public async Task Una_contrasena_incorrecta_responde_no_valida_sin_ser_un_error()
    {
        var (caso, identidad) = Preparar();
        identidad.ResultadoVerificarPassword = Result.Fail(KeycloakMensajes.PasswordActualIncorrecta);

        var resultado = await caso.EjecutarAsync(new("Equivocada#1"));

        Assert.True(resultado.IsOk);
        Assert.False(resultado.Value.Valida);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Una_contrasena_vacia_no_valida_y_no_consulta_a_keycloak(string? actual)
    {
        var (caso, identidad) = Preparar();

        var resultado = await caso.EjecutarAsync(new(actual));

        Assert.True(resultado.IsOk);
        Assert.False(resultado.Value.Valida);
        Assert.Empty(identidad.Llamadas);
    }

    [Fact]
    public async Task Si_el_servicio_de_identidad_falla_devuelve_el_error_y_no_una_respuesta_negativa()
    {
        var (caso, identidad) = Preparar();
        identidad.ResultadoVerificarPassword = Result.Fail(KeycloakMensajes.ServicioNoDisponible);

        var resultado = await caso.EjecutarAsync(new("Actual#123"));

        Assert.True(resultado.IsFail);
        Assert.Equal(KeycloakMensajes.ServicioNoDisponible, resultado.Error);
    }

    [Fact]
    public async Task Solo_verifica_la_cuenta_del_usuario_autenticado()
    {
        var identidad = new GestorDeIdentidadFalso();
        var caso = new VerificarPasswordActualCasoDeUso(identidad, new UsuarioActualStub("otra.persona"));

        await caso.EjecutarAsync(new("Actual#123"));

        Assert.Equal("otra.persona", Assert.Single(identidad.Verificaciones).Username);
    }

    [Fact]
    public void El_mensaje_de_keycloak_para_contrasena_incorrecta_es_el_de_la_aplicacion()
    {
        Assert.Equal(MensajesDeGestion.PasswordActualIncorrecta, KeycloakMensajes.PasswordActualIncorrecta);
    }

    private static PerfilController Controlador(GestorDeIdentidadFalso identidad) =>
        new(null!, null!, new VerificarPasswordActualCasoDeUso(identidad, new UsuarioActualStub(Username)));

    [Fact]
    public async Task El_endpoint_responde_200_con_valida_en_verdadero()
    {
        var accion = await Controlador(new GestorDeIdentidadFalso())
            .VerificarPasswordActual(new VerificarPasswordActualRequest("Actual#123"));

        var ok = Assert.IsType<OkObjectResult>(accion.Result);
        Assert.True(Assert.IsType<VerificarPasswordActualRespuesta>(ok.Value).Valida);
    }

    [Fact]
    public async Task El_endpoint_responde_200_con_valida_en_falso_si_la_contrasena_es_incorrecta()
    {
        var identidad = new GestorDeIdentidadFalso { ResultadoVerificarPassword = Result.Fail(KeycloakMensajes.PasswordActualIncorrecta) };

        var accion = await Controlador(identidad).VerificarPasswordActual(new VerificarPasswordActualRequest("Mal#1234"));

        var ok = Assert.IsType<OkObjectResult>(accion.Result);
        Assert.False(Assert.IsType<VerificarPasswordActualRespuesta>(ok.Value).Valida);
    }

    [Fact]
    public async Task El_endpoint_sin_cuerpo_responde_200_con_valida_en_falso()
    {
        var accion = await Controlador(new GestorDeIdentidadFalso()).VerificarPasswordActual(null);

        var ok = Assert.IsType<OkObjectResult>(accion.Result);
        Assert.False(Assert.IsType<VerificarPasswordActualRespuesta>(ok.Value).Valida);
    }

    [Fact]
    public async Task El_endpoint_responde_503_con_error_si_la_identidad_no_esta_disponible()
    {
        var identidad = new GestorDeIdentidadFalso { ResultadoVerificarPassword = Result.Fail(KeycloakMensajes.ServicioNoDisponible) };

        var accion = await Controlador(identidad).VerificarPasswordActual(new VerificarPasswordActualRequest("Actual#123"));

        var objeto = Assert.IsType<ObjectResult>(accion.Result);
        Assert.Equal(503, objeto.StatusCode);
        Assert.Equal(KeycloakMensajes.ServicioNoDisponible, Assert.IsType<ErrorRespuesta>(objeto.Value).Error);
    }

    [Fact]
    public void El_endpoint_exige_autenticacion_y_no_es_anonimo()
    {
        var tipo = typeof(PerfilController);
        var metodo = tipo.GetMethod(nameof(PerfilController.VerificarPasswordActual))!;

        Assert.Contains(tipo.GetCustomAttributes(true), a => a is Microsoft.AspNetCore.Authorization.AuthorizeAttribute);
        Assert.DoesNotContain(metodo.GetCustomAttributes(true), a => a is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute);
        Assert.Equal("password/verificacion", metodo.GetCustomAttributes(true).OfType<HttpPostAttribute>().Single().Template);
    }
}
