using PuntoMarket.Usuarios.Application.UseCases.Gestion.ObtenerPerfil;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class ObtenerPerfilTests
{
    private static readonly DateTime Creacion = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);
    private const int RolCajero = 2;

    private static Usuario UsuarioDePrueba(bool activo = true, bool debeCambiarPassword = true) =>
        Usuario.Reconstruir(
            5, "aperez", "ana@correo.com", "Ana Pérez", RolCajero,
            activo, debeCambiarPassword, "admin", Creacion, null, null);

    [Fact]
    public async Task Devuelve_el_perfil_con_el_rol_que_guarda_la_tabla()
    {
        var repositorio = new UsuarioRepositorioStub
        {
            UsuarioEncontrado = UsuarioDePrueba(),
            RolEncontrado = Rol.Reconstruir(RolCajero, "Cajero", activo: true)
        };
        var caso = new ObtenerPerfilCasoDeUso(repositorio, new UsuarioActualStub("aperez"));

        var resultado = await caso.EjecutarAsync(new());

        Assert.True(resultado.IsOk);
        Assert.Equal("aperez", resultado.Value.Username);
        Assert.Equal("Ana Pérez", resultado.Value.NombreCompleto);
        Assert.Equal("Cajero", resultado.Value.Rol);
        Assert.True(resultado.Value.Activo);
        Assert.True(resultado.Value.DebeCambiarPassword);
    }

    [Fact]
    public async Task Busca_por_el_username_del_token_y_el_rol_del_usuario()
    {
        var repositorio = new UsuarioRepositorioStub
        {
            UsuarioEncontrado = UsuarioDePrueba(),
            RolEncontrado = Rol.Reconstruir(RolCajero, "Cajero", activo: true)
        };
        var caso = new ObtenerPerfilCasoDeUso(repositorio, new UsuarioActualStub("aperez"));

        await caso.EjecutarAsync(new());

        Assert.Equal("aperez", Assert.Single(repositorio.UsernamesConsultados));
        Assert.Equal(RolCajero, Assert.Single(repositorio.RolesConsultados));
    }

    [Fact]
    public async Task Falla_cuando_el_usuario_del_token_no_esta_en_la_tabla()
    {
        var repositorio = new UsuarioRepositorioStub();
        var caso = new ObtenerPerfilCasoDeUso(repositorio, new UsuarioActualStub());

        var resultado = await caso.EjecutarAsync(new());

        Assert.True(resultado.IsFail);
        Assert.Equal(ObtenerPerfilCasoDeUso.UsuarioNoRegistrado, resultado.Error);
        Assert.Empty(repositorio.RolesConsultados);
    }

    [Fact]
    public async Task Falla_cuando_el_rol_guardado_ya_no_existe()
    {
        var repositorio = new UsuarioRepositorioStub { UsuarioEncontrado = UsuarioDePrueba() };
        var caso = new ObtenerPerfilCasoDeUso(repositorio, new UsuarioActualStub("aperez"));

        var resultado = await caso.EjecutarAsync(new());

        Assert.True(resultado.IsFail);
        Assert.Equal(ObtenerPerfilCasoDeUso.RolNoDisponible, resultado.Error);
    }

    [Fact]
    public async Task Informa_el_estado_sin_decidir_si_puede_entrar()
    {
        var repositorio = new UsuarioRepositorioStub
        {
            UsuarioEncontrado = UsuarioDePrueba(activo: false, debeCambiarPassword: false),
            RolEncontrado = Rol.Reconstruir(RolCajero, "Cajero", activo: true)
        };
        var caso = new ObtenerPerfilCasoDeUso(repositorio, new UsuarioActualStub("aperez"));

        var resultado = await caso.EjecutarAsync(new());

        Assert.True(resultado.IsOk);
        Assert.False(resultado.Value.Activo);
        Assert.False(resultado.Value.DebeCambiarPassword);
    }
}
