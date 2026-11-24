using PuntoMarket.Usuarios.Application.UseCases.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class ListarUsuariosTests
{
    private static readonly DateTime Creacion = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Usuario UsuarioDePrueba(int id, string username, int rolId, bool activo = true) =>
        Usuario.Reconstruir(
            id, username, $"{username}@correo.com", "Ana", "Pérez", null, rolId,
            activo, debeCambiarPassword: false, "admin", Creacion, null, null);

    [Fact]
    public async Task Devuelve_cada_usuario_con_el_nombre_de_su_rol()
    {
        var repositorio = new UsuarioRepositorioStub
        {
            Usuarios = [UsuarioDePrueba(1, "aperez", 1), UsuarioDePrueba(2, "lgomez", 2, activo: false)],
            Roles =
            [
                Rol.Reconstruir(1, "Administrador", activo: true),
                Rol.Reconstruir(2, "Cajero", activo: true)
            ]
        };
        var caso = new ListarUsuariosCasoDeUso(repositorio);

        var resultado = await caso.EjecutarAsync(new());

        Assert.True(resultado.IsOk);
        Assert.Collection(
            resultado.Value.Usuarios,
            primero =>
            {
                Assert.Equal("aperez", primero.Username);
                Assert.Equal("Administrador", primero.Rol);
                Assert.True(primero.Activo);
            },
            segundo =>
            {
                Assert.Equal("lgomez", segundo.Username);
                Assert.Equal("Cajero", segundo.Rol);
                Assert.False(segundo.Activo);
            });
    }

    [Fact]
    public async Task Resuelve_los_roles_con_una_sola_consulta_aunque_haya_varios_usuarios()
    {
        var repositorio = new UsuarioRepositorioStub
        {
            Usuarios = [UsuarioDePrueba(1, "aperez", 1), UsuarioDePrueba(2, "lgomez", 1), UsuarioDePrueba(3, "msoto", 1)],
            Roles = [Rol.Reconstruir(1, "Cajero", activo: true)]
        };
        var caso = new ListarUsuariosCasoDeUso(repositorio);

        await caso.EjecutarAsync(new());

        Assert.Empty(repositorio.RolesConsultados);
    }

    [Fact]
    public async Task Sin_usuarios_devuelve_una_lista_vacia()
    {
        var caso = new ListarUsuariosCasoDeUso(new UsuarioRepositorioStub());

        var resultado = await caso.EjecutarAsync(new());

        Assert.True(resultado.IsOk);
        Assert.Empty(resultado.Value.Usuarios);
    }

    [Fact]
    public async Task Si_el_rol_guardado_ya_no_existe_lo_informa_en_vez_de_inventarlo()
    {
        var repositorio = new UsuarioRepositorioStub { Usuarios = [UsuarioDePrueba(1, "aperez", 9)] };
        var caso = new ListarUsuariosCasoDeUso(repositorio);

        var resultado = await caso.EjecutarAsync(new());

        Assert.Equal(MensajesDeGestion.SinRolAsignado, Assert.Single(resultado.Value.Usuarios).Rol);
    }
}
