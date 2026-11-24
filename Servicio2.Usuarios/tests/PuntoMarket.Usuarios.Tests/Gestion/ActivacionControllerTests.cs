using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Usuarios.Api.Contracts;
using PuntoMarket.Usuarios.Api.Contracts.Gestion;
using PuntoMarket.Usuarios.Api.Controllers;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ActivarCuenta;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class ActivacionControllerTests
{
    private const string Token = "tok-1";
    private const string Password = "NuevaClave#2026";
    private static readonly DateTime Creacion = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    private static (ActivacionController Controlador, GestorDeIdentidadFalso Identidad) Preparar()
    {
        var usuario = Usuario.Reconstruir(7, "aperez", "ana@correo.com", "Ana Pérez", 2, true, true, "admin", Creacion, null, null);
        var reloj = new RelojFijo();
        var activaciones = new ActivacionRepositorioFalso();
        activaciones.Usuarios[7] = usuario;
        activaciones.Tokens[7] = ("hash:" + Token, reloj.Ahora.AddMinutes(60));
        var identidad = new GestorDeIdentidadFalso();
        var caso = new ActivarCuentaCasoDeUso(
            activaciones,
            new UsuarioRepositorioStub { UsuarioEncontrado = usuario },
            identidad,
            new GeneradorTokenFalso(),
            reloj);

        return (new ActivacionController(caso), identidad);
    }

    [Fact]
    public async Task Responde_200_con_activada_en_verdadero()
    {
        var (controlador, _) = Preparar();

        var accion = await controlador.Activar(new ActivarCuentaSolicitud(Token, Password, Password), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(accion.Result);
        Assert.True(Assert.IsType<ActivarCuentaRespuesta>(ok.Value).Activada);
    }

    [Fact]
    public async Task Responde_400_con_el_mensaje_generico_si_el_token_no_sirve()
    {
        var (controlador, _) = Preparar();

        var accion = await controlador.Activar(new ActivarCuentaSolicitud("otro", Password, Password), CancellationToken.None);

        var invalida = Assert.IsType<BadRequestObjectResult>(accion.Result);
        Assert.Equal(ActivarCuentaCasoDeUso.EnlaceInvalido, Assert.IsType<ErrorRespuesta>(invalida.Value).Error);
    }

    [Fact]
    public async Task Reutilizar_el_token_responde_400_con_el_mismo_mensaje()
    {
        var (controlador, _) = Preparar();
        await controlador.Activar(new ActivarCuentaSolicitud(Token, Password, Password), CancellationToken.None);

        var accion = await controlador.Activar(new ActivarCuentaSolicitud(Token, Password, Password), CancellationToken.None);

        var invalida = Assert.IsType<BadRequestObjectResult>(accion.Result);
        Assert.Equal(ActivarCuentaCasoDeUso.EnlaceInvalido, Assert.IsType<ErrorRespuesta>(invalida.Value).Error);
    }

    [Fact]
    public async Task Responde_400_con_el_motivo_si_la_contrasena_no_cumple_la_politica()
    {
        var (controlador, _) = Preparar();

        var accion = await controlador.Activar(new ActivarCuentaSolicitud(Token, "debil", "debil"), CancellationToken.None);

        var invalida = Assert.IsType<BadRequestObjectResult>(accion.Result);
        Assert.Contains("al menos 8 caracteres", Assert.IsType<ErrorRespuesta>(invalida.Value).Error);
    }

    [Fact]
    public async Task Responde_503_si_la_identidad_no_esta_disponible()
    {
        var (controlador, identidad) = Preparar();
        identidad.ResultadoCambiarPassword = Result.Fail("No disponible.");

        var accion = await controlador.Activar(new ActivarCuentaSolicitud(Token, Password, Password), CancellationToken.None);

        var respuesta = Assert.IsType<ObjectResult>(accion.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal(ActivarCuentaCasoDeUso.ActivacionNoCompletada, Assert.IsType<ErrorRespuesta>(respuesta.Value).Error);
    }

    [Fact]
    public async Task Un_cuerpo_vacio_responde_400_con_el_mensaje_generico()
    {
        var (controlador, _) = Preparar();

        var accion = await controlador.Activar(null, CancellationToken.None);

        var invalida = Assert.IsType<BadRequestObjectResult>(accion.Result);
        Assert.Equal(ActivarCuentaCasoDeUso.EnlaceInvalido, Assert.IsType<ErrorRespuesta>(invalida.Value).Error);
    }

    [Fact]
    public void La_ruta_es_post_api_activacion_y_es_anonima()
    {
        var tipo = typeof(ActivacionController);
        var ruta = Assert.Single(tipo.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>());
        var metodo = tipo.GetMethod(nameof(ActivacionController.Activar))!;

        Assert.Equal("api/activacion", ruta.Template);
        Assert.Single(metodo.GetCustomAttributes(typeof(HttpPostAttribute), false));
        Assert.Single(tipo.GetCustomAttributes(typeof(AllowAnonymousAttribute), false));
        Assert.Empty(tipo.GetCustomAttributes(typeof(AuthorizeAttribute), false));
    }
}
