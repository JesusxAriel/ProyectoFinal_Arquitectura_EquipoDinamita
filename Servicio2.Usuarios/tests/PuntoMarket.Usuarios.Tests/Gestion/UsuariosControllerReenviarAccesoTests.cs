using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Usuarios.Api.Contracts;
using PuntoMarket.Usuarios.Api.Contracts.Gestion;
using PuntoMarket.Usuarios.Api.Controllers;
using PuntoMarket.Usuarios.Application.UseCases.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ReenviarAcceso;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class UsuariosControllerReenviarAccesoTests
{
    private static readonly DateTime Creacion = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Usuario UsuarioDePrueba(bool activo = true) =>
        Usuario.Reconstruir(7, "aperez", "ana@correo.com", "Ana", "Pérez", null, 2, activo, false, "admin", Creacion, null, null);

    private static (UsuariosController Controlador, EnviadorCorreoSpy Correo) Preparar(Usuario? usuario)
    {
        var repositorio = new UsuarioRepositorioStub { UsuarioEncontrado = usuario };
        var correo = new EnviadorCorreoSpy();
        var caso = new ReenviarAccesoCasoDeUso(
            repositorio,
            new GestorDeIdentidadFalso(),
            correo,
            new GeneradorPasswordFalso(),
            new UsuarioActualStub("admin"),
            new RelojFijo());

        var controlador = new UsuariosController(null!, null!, null!, null!, null!, caso, repositorio);
        return (controlador, correo);
    }

    [Fact]
    public async Task Responde_200_con_correo_enviado_en_verdadero()
    {
        var (controlador, _) = Preparar(UsuarioDePrueba());

        var accion = await controlador.ReenviarAcceso(7, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(accion.Result);
        Assert.True(Assert.IsType<ReenviarAccesoRespuesta>(ok.Value).CorreoEnviado);
    }

    [Fact]
    public async Task Responde_200_con_correo_enviado_en_falso_si_el_correo_falla()
    {
        var (controlador, correo) = Preparar(UsuarioDePrueba());
        correo.Resultado = Result.Fail("Sin servidor SMTP.");

        var accion = await controlador.ReenviarAcceso(7, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(accion.Result);
        Assert.False(Assert.IsType<ReenviarAccesoRespuesta>(ok.Value).CorreoEnviado);
    }

    [Fact]
    public async Task Responde_404_con_error_si_el_usuario_no_existe()
    {
        var (controlador, _) = Preparar(null);

        var accion = await controlador.ReenviarAcceso(99, CancellationToken.None);

        var noEncontrado = Assert.IsType<NotFoundObjectResult>(accion.Result);
        Assert.Equal(MensajesDeGestion.UsuarioNoEncontrado, Assert.IsType<ErrorRespuesta>(noEncontrado.Value).Error);
    }

    [Fact]
    public async Task Responde_400_con_error_si_el_usuario_esta_inactivo()
    {
        var (controlador, _) = Preparar(UsuarioDePrueba(activo: false));

        var accion = await controlador.ReenviarAcceso(7, CancellationToken.None);

        var invalida = Assert.IsType<BadRequestObjectResult>(accion.Result);
        Assert.Equal(ReenviarAccesoCasoDeUso.UsuarioInactivo, Assert.IsType<ErrorRespuesta>(invalida.Value).Error);
    }

    [Fact]
    public void La_ruta_es_post_y_el_controlador_exige_rol_administrador()
    {
        var metodo = typeof(UsuariosController).GetMethod(nameof(UsuariosController.ReenviarAcceso))!;
        var ruta = Assert.Single(metodo.GetCustomAttributes(typeof(HttpPostAttribute), false).Cast<HttpPostAttribute>());
        var autorizacion = Assert.Single(
            typeof(UsuariosController).GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>());

        Assert.Equal("{id:int}/reenviar-acceso", ruta.Template);
        Assert.Equal("Administrador", autorizacion.Roles);
    }
}
