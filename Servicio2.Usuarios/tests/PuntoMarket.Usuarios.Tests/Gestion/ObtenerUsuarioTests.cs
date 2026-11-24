using PuntoMarket.Usuarios.Application.UseCases.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ObtenerUsuario;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class ObtenerUsuarioTests
{
    private const int RolCajero = 2;
    private static readonly DateTime Creacion = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Usuario UsuarioDePrueba() =>
        Usuario.Reconstruir(
            7, "aperez", "ana@correo.com", "Ana", "Pérez", null, RolCajero,
            activo: true, debeCambiarPassword: true, "admin", Creacion, null, null);

    [Fact]
    public async Task Devuelve_el_usuario_con_su_rol_y_sin_datos_de_contrasena()
    {
        var repositorio = new UsuarioRepositorioStub
        {
            UsuarioEncontrado = UsuarioDePrueba(),
            RolEncontrado = Rol.Reconstruir(RolCajero, "Cajero", activo: true)
        };
        var caso = new ObtenerUsuarioCasoDeUso(repositorio);

        var resultado = await caso.EjecutarAsync(new(7));

        Assert.True(resultado.IsOk);
        var usuario = resultado.Value.Usuario;
        Assert.Equal(7, usuario.Id);
        Assert.Equal("aperez", usuario.Username);
        Assert.Equal("ana@correo.com", usuario.Correo);
        Assert.Equal("Cajero", usuario.Rol);
        Assert.True(usuario.Activo);
        Assert.True(usuario.DebeCambiarPassword);
        Assert.Equal(7, Assert.Single(repositorio.IdsConsultados));
    }

    [Fact]
    public async Task Falla_cuando_el_usuario_no_existe()
    {
        var caso = new ObtenerUsuarioCasoDeUso(new UsuarioRepositorioStub());

        var resultado = await caso.EjecutarAsync(new(99));

        Assert.True(resultado.IsFail);
        Assert.Equal(MensajesDeGestion.UsuarioNoEncontrado, resultado.Error);
    }

    [Fact]
    public async Task Si_el_rol_ya_no_existe_lo_informa_en_vez_de_fallar()
    {
        var repositorio = new UsuarioRepositorioStub { UsuarioEncontrado = UsuarioDePrueba() };
        var caso = new ObtenerUsuarioCasoDeUso(repositorio);

        var resultado = await caso.EjecutarAsync(new(7));

        Assert.True(resultado.IsOk);
        Assert.Equal(MensajesDeGestion.SinRolAsignado, resultado.Value.Usuario.Rol);
    }
}
