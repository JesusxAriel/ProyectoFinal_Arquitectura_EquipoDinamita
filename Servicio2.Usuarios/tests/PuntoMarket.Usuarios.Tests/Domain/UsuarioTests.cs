using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Domain;

public class UsuarioTests
{
    private static readonly DateTime Ahora = new(2026, 11, 19, 15, 30, 0, DateTimeKind.Utc);
    private const string Responsable = "admin";
    private const int RolCajero = 2;

    private static Result<Usuario> CrearValido() =>
        Usuario.Crear("Juan Carlos", "Pérez Rojas", "Juan@Correo.com", RolCajero, "jperez", Responsable, Ahora);

    [Fact]
    public void Crear_con_datos_validos_normaliza_y_deja_el_usuario_pendiente_de_cambiar_password()
    {
        var resultado = CrearValido();

        Assert.True(resultado.IsOk);
        var usuario = resultado.Value;
        Assert.Equal("jperez", usuario.Username);
        Assert.Equal("Juan Carlos Pérez Rojas", usuario.NombreCompleto);
        Assert.Equal("juan@correo.com", usuario.Correo);
        Assert.Equal(RolCajero, usuario.RolId);
        Assert.True(usuario.Activo);
        Assert.True(usuario.DebeCambiarPassword);
        Assert.Equal(Responsable, usuario.UsuarioCreacion);
        Assert.Equal(Ahora, usuario.FechaCreacion);
        Assert.Null(usuario.UsuarioModificacion);
        Assert.Null(usuario.FechaModificacion);
    }

    [Theory]
    [InlineData(null, "Pérez", "juan@correo.com", RolCajero, "jperez")]
    [InlineData("Juan", "   ", "juan@correo.com", RolCajero, "jperez")]
    [InlineData("Juan", "Pérez", "juan@correo", RolCajero, "jperez")]
    [InlineData("Juan", "Pérez", "juan@correo.com", 0, "jperez")]
    [InlineData("Juan", "Pérez", "juan@correo.com", RolCajero, "")]
    public void Crear_con_datos_invalidos_no_construye_el_objeto(
        string? nombres, string? apellidos, string? correo, int rolId, string? username)
    {
        var resultado = Usuario.Crear(nombres, apellidos, correo, rolId, username, Responsable, Ahora);

        Assert.True(resultado.IsFail);
        Assert.Null(resultado.Value);
        Assert.NotEmpty(resultado.Error);
    }

    [Fact]
    public void Crear_sin_responsable_de_auditoria_falla()
    {
        var resultado = Usuario.Crear(
            "Juan", "Pérez", "juan@correo.com", RolCajero, "jperez", "   ", Ahora);

        Assert.True(resultado.IsFail);
    }

    [Fact]
    public void Crear_rechaza_un_nombre_completo_mas_largo_que_la_columna()
    {
        var apellidoLargo = new string('a', Usuario.LongitudMaximaNombreCompleto);

        var resultado = Usuario.Crear(
            "Juan", apellidoLargo, "juan@correo.com", RolCajero, "jperez", Responsable, Ahora);

        Assert.True(resultado.IsFail);
    }

    [Fact]
    public void ConIdAsignado_conserva_los_datos_y_agrega_el_id()
    {
        var usuario = CrearValido().Value.ConIdAsignado(9);

        Assert.Equal(9, usuario.Id);
        Assert.Equal("jperez", usuario.Username);
        Assert.Equal("juan@correo.com", usuario.Correo);
    }

    [Fact]
    public void Actualizar_cambia_los_datos_y_registra_la_auditoria()
    {
        var usuario = CrearValido().Value;
        var despues = Ahora.AddHours(1);

        var resultado = usuario.Actualizar(
            "Juana", "Pérez Soto", "Juana@Correo.com", RolCajero, "ale", despues);

        Assert.True(resultado.IsOk);
        Assert.Equal("Juana Pérez Soto", usuario.NombreCompleto);
        Assert.Equal("juana@correo.com", usuario.Correo);
        Assert.Equal("ale", usuario.UsuarioModificacion);
        Assert.Equal(despues, usuario.FechaModificacion);
    }

    [Fact]
    public void Actualizar_con_datos_invalidos_no_modifica_el_usuario()
    {
        var usuario = CrearValido().Value;

        var resultado = usuario.Actualizar("Juana", "Pérez", "sin-arroba", RolCajero, "ale", Ahora);

        Assert.True(resultado.IsFail);
        Assert.Equal("Juan Carlos Pérez Rojas", usuario.NombreCompleto);
        Assert.Null(usuario.UsuarioModificacion);
    }

    [Fact]
    public void CambiarEstado_da_de_baja_y_registra_la_auditoria()
    {
        var usuario = CrearValido().Value;

        var resultado = usuario.CambiarEstado(false, "ale", Ahora);

        Assert.True(resultado.IsOk);
        Assert.False(usuario.Activo);
        Assert.Equal("ale", usuario.UsuarioModificacion);
    }

    [Fact]
    public void CambiarEstado_al_mismo_estado_falla_sin_tocar_la_auditoria()
    {
        var usuario = CrearValido().Value;

        var resultado = usuario.CambiarEstado(true, "ale", Ahora);

        Assert.True(resultado.IsFail);
        Assert.Equal("El usuario ya se encuentra activo.", resultado.Error);
        Assert.Null(usuario.UsuarioModificacion);
    }

    [Fact]
    public void PasswordCambiada_libera_el_cambio_obligatorio()
    {
        var usuario = CrearValido().Value;

        var resultado = usuario.PasswordCambiada("jperez", Ahora);

        Assert.True(resultado.IsOk);
        Assert.False(usuario.DebeCambiarPassword);
        Assert.Equal("jperez", usuario.UsuarioModificacion);
    }

    [Fact]
    public void AccesoRestablecido_exige_el_cambio_de_password_y_registra_la_auditoria()
    {
        var usuario = Usuario.Reconstruir(
            4, "jperez", "juan@correo.com", "Juan Pérez", RolCajero,
            activo: true, debeCambiarPassword: false, "admin", Ahora, null, null);

        var resultado = usuario.AccesoRestablecido("admin", Ahora.AddDays(1));

        Assert.True(resultado.IsOk);
        Assert.True(usuario.DebeCambiarPassword);
        Assert.Equal("admin", usuario.UsuarioModificacion);
        Assert.Equal(Ahora.AddDays(1), usuario.FechaModificacion);
    }

    [Fact]
    public void AccesoRestablecido_rechaza_un_responsable_vacio()
    {
        var usuario = Usuario.Reconstruir(
            4, "jperez", "juan@correo.com", "Juan Pérez", RolCajero,
            activo: true, debeCambiarPassword: false, "admin", Ahora, null, null);

        var resultado = usuario.AccesoRestablecido(" ", Ahora);

        Assert.True(resultado.IsFail);
        Assert.False(usuario.DebeCambiarPassword);
    }

    [Fact]
    public void Reconstruir_devuelve_el_usuario_tal_como_esta_en_la_base()
    {
        var usuario = Usuario.Reconstruir(
            4, "jperez", "juan@correo.com", "Juan Pérez", RolCajero,
            activo: false, debeCambiarPassword: false, "admin", Ahora, "ale", Ahora.AddDays(1));

        Assert.Equal(4, usuario.Id);
        Assert.False(usuario.Activo);
        Assert.False(usuario.DebeCambiarPassword);
        Assert.Equal("ale", usuario.UsuarioModificacion);
        Assert.Equal(Ahora.AddDays(1), usuario.FechaModificacion);
    }
}
