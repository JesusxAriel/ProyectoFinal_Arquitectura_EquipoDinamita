using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using PuntoMarket.Usuarios.Api.Contracts;
using PuntoMarket.Usuarios.Api.Controllers;
using PuntoMarket.Usuarios.Api.Filters;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ConsultarCambioObligatorio;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Seguridad;

public class CambioPasswordObligatorioFilterTests
{
    private const string Username = "aperez";
    private static readonly DateTime Creacion = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Usuario UsuarioDePrueba(bool debeCambiarPassword) =>
        Usuario.Reconstruir(7, Username, "ana@correo.com", "Ana", "Pérez", null, 2, true, debeCambiarPassword, "admin", Creacion, null, null);

    private static CambioPasswordObligatorioFilter CrearFiltro(Usuario? usuario) =>
        new(new ConsultarCambioObligatorioCasoDeUso(
            new UsuarioRepositorioStub { UsuarioEncontrado = usuario },
            new UsuarioActualStub(Username)));

    private static async Task<(ActionExecutingContext Contexto, bool SeEjecuto)> Ejecutar(
        CambioPasswordObligatorioFilter filtro, Type controlador, string accion, bool autenticado = true)
    {
        var http = new DefaultHttpContext
        {
            User = autenticado
                ? new ClaimsPrincipal(new ClaimsIdentity([new Claim("preferred_username", Username)], "Bearer"))
                : new ClaimsPrincipal(new ClaimsIdentity())
        };
        var descriptor = new ControllerActionDescriptor
        {
            MethodInfo = controlador.GetMethod(accion, BindingFlags.Public | BindingFlags.Instance)!
        };
        var contexto = new ActionExecutingContext(
            new ActionContext(http, new RouteData(), descriptor), [], new Dictionary<string, object?>(), controller: new object());

        var seEjecuto = false;
        await filtro.OnActionExecutionAsync(contexto, () =>
        {
            seEjecuto = true;
            return Task.FromResult(new ActionExecutedContext(contexto, [], controller: new object()));
        });

        return (contexto, seEjecuto);
    }

    [Fact]
    public async Task Con_cambio_pendiente_un_endpoint_de_gestion_responde_403_con_el_mensaje()
    {
        var (contexto, seEjecuto) = await Ejecutar(
            CrearFiltro(UsuarioDePrueba(true)), typeof(UsuariosController), nameof(UsuariosController.Listar));

        var resultado = Assert.IsType<ObjectResult>(contexto.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, resultado.StatusCode);
        Assert.Equal(
            CambioPasswordObligatorioFilter.MensajeCambioObligatorio,
            Assert.IsType<ErrorRespuesta>(resultado.Value).Error);
        Assert.False(seEjecuto);
    }

    [Theory]
    [InlineData(nameof(PerfilController.Obtener))]
    [InlineData(nameof(PerfilController.VerificarPasswordActual))]
    [InlineData(nameof(PerfilController.CambiarPassword))]
    public async Task Con_cambio_pendiente_los_tres_endpoints_de_perfil_siguen_disponibles(string accion)
    {
        var (contexto, seEjecuto) = await Ejecutar(
            CrearFiltro(UsuarioDePrueba(true)), typeof(PerfilController), accion);

        Assert.Null(contexto.Result);
        Assert.True(seEjecuto);
    }

    [Fact]
    public async Task Sin_cambio_pendiente_el_endpoint_de_gestion_se_ejecuta_normal()
    {
        var (contexto, seEjecuto) = await Ejecutar(
            CrearFiltro(UsuarioDePrueba(false)), typeof(UsuariosController), nameof(UsuariosController.Listar));

        Assert.Null(contexto.Result);
        Assert.True(seEjecuto);
    }

    [Fact]
    public async Task Si_el_usuario_no_existe_en_la_tabla_deja_pasar_el_flujo_normal()
    {
        var (contexto, seEjecuto) = await Ejecutar(
            CrearFiltro(null), typeof(UsuariosController), nameof(UsuariosController.Listar));

        Assert.Null(contexto.Result);
        Assert.True(seEjecuto);
    }

    [Fact]
    public async Task Sin_autenticar_no_consulta_nada_y_deja_pasar()
    {
        var (contexto, seEjecuto) = await Ejecutar(
            CrearFiltro(UsuarioDePrueba(true)), typeof(UsuariosController), nameof(UsuariosController.Listar), autenticado: false);

        Assert.Null(contexto.Result);
        Assert.True(seEjecuto);
    }
}
