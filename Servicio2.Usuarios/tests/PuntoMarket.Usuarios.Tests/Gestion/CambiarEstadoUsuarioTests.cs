using PuntoMarket.Usuarios.Application.UseCases.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CambiarEstadoUsuario;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Infrastructure.Identity;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class CambiarEstadoUsuarioTests
{
    private const int RolCajero = 2;
    private const string Administrador = "admin";
    private static readonly DateTime Creacion = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Usuario UsuarioDePrueba(string username = "aperez", bool activo = true) =>
        Usuario.Reconstruir(
            7, username, "ana@correo.com", "Ana Pérez", RolCajero,
            activo, debeCambiarPassword: false, Administrador, Creacion, null, null);

    private sealed record Escenario(
        CambiarEstadoUsuarioCasoDeUso Caso,
        UsuarioRepositorioStub Repositorio,
        GestorDeIdentidadFalso Identidad,
        RelojFijo Reloj);

    private static Escenario Preparar(Usuario? usuario = null)
    {
        var repositorio = new UsuarioRepositorioStub { UsuarioEncontrado = usuario ?? UsuarioDePrueba() };
        var identidad = new GestorDeIdentidadFalso();
        var reloj = new RelojFijo();

        return new Escenario(
            new CambiarEstadoUsuarioCasoDeUso(
                repositorio, identidad, new UsuarioActualStub(Administrador), reloj),
            repositorio,
            identidad,
            reloj);
    }

    [Fact]
    public async Task La_baja_logica_desactiva_en_keycloak_y_despues_en_la_tabla()
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(new(7, Activo: false));

        Assert.True(resultado.IsOk);
        Assert.False(resultado.Value.Activo);
        Assert.Equal(("aperez", false), Assert.Single(escenario.Identidad.Estados));

        var guardado = Assert.Single(escenario.Repositorio.Actualizados);
        Assert.False(guardado.Activo);
        Assert.Equal(Administrador, guardado.UsuarioModificacion);
        Assert.Equal(escenario.Reloj.Ahora, guardado.FechaModificacion);
    }

    [Fact]
    public async Task Reactiva_al_usuario_dado_de_baja()
    {
        var escenario = Preparar(UsuarioDePrueba(activo: false));

        var resultado = await escenario.Caso.EjecutarAsync(new(7, Activo: true));

        Assert.True(resultado.IsOk);
        Assert.True(resultado.Value.Activo);
        Assert.Equal(("aperez", true), Assert.Single(escenario.Identidad.Estados));
        Assert.True(Assert.Single(escenario.Repositorio.Actualizados).Activo);
    }

    [Fact]
    public async Task Rechaza_dejarlo_en_el_estado_que_ya_tiene_sin_tocar_keycloak()
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(new(7, Activo: true));

        Assert.True(resultado.IsFail);
        Assert.Equal("El usuario ya se encuentra activo.", resultado.Error);
        Assert.Empty(escenario.Identidad.Llamadas);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Fact]
    public async Task No_permite_que_el_administrador_se_desactive_a_si_mismo()
    {
        var escenario = Preparar(UsuarioDePrueba(username: Administrador));

        var resultado = await escenario.Caso.EjecutarAsync(new(7, Activo: false));

        Assert.True(resultado.IsFail);
        Assert.Equal(CambiarEstadoUsuarioCasoDeUso.NoPuedeDesactivarseASiMismo, resultado.Error);
        Assert.Empty(escenario.Identidad.Llamadas);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Fact]
    public async Task Falla_cuando_el_usuario_no_existe()
    {
        var escenario = Preparar();
        escenario.Repositorio.UsuarioEncontrado = null;

        var resultado = await escenario.Caso.EjecutarAsync(new(99, Activo: false));

        Assert.True(resultado.IsFail);
        Assert.Equal(MensajesDeGestion.UsuarioNoEncontrado, resultado.Error);
        Assert.Empty(escenario.Identidad.Llamadas);
    }

    [Fact]
    public async Task No_toca_la_tabla_si_keycloak_no_pudo_cambiar_el_estado()
    {
        var escenario = Preparar();
        escenario.Identidad.ResultadoCambiarEstado = Result.Fail(KeycloakMensajes.ServicioNoDisponible);

        var resultado = await escenario.Caso.EjecutarAsync(new(7, Activo: false));

        Assert.True(resultado.IsFail);
        Assert.Equal(KeycloakMensajes.ServicioNoDisponible, resultado.Error);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Fact]
    public async Task Informa_si_no_se_pudo_guardar_el_estado_en_la_tabla()
    {
        var escenario = Preparar();
        escenario.Repositorio.ResultadoActualizar = Result<Usuario>.Fail(MensajesDeGestion.UsuarioNoEncontrado);

        var resultado = await escenario.Caso.EjecutarAsync(new(7, Activo: false));

        Assert.True(resultado.IsFail);
        Assert.Equal(MensajesDeGestion.UsuarioNoEncontrado, resultado.Error);
        Assert.Single(escenario.Identidad.Estados);
    }
}
