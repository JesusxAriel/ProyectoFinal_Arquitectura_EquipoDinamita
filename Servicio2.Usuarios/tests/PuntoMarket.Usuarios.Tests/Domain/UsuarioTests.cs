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
        Usuario.Crear("Juan Carlos", "Pérez", "Rojas", "Juan@Correo.com", RolCajero, "jperez", Responsable, Ahora);

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
    [InlineData(null, "Pérez", "Rojas", "juan@correo.com", RolCajero, "jperez")]
    [InlineData("Juan", "   ", "Rojas", "juan@correo.com", RolCajero, "jperez")]
    [InlineData("Juan", null, null, "juan@correo.com", RolCajero, "jperez")]
    [InlineData("Juan", "Pérez", "Ro\tjas", "juan@correo.com", RolCajero, "jperez")]
    [InlineData("Juan", "Pérez", "Rojas", "juan@correo", RolCajero, "jperez")]
    [InlineData("Juan", "Pérez", "Rojas", "juan@correo.com", 0, "jperez")]
    [InlineData("Juan", "Pérez", "Rojas", "juan@correo.com", RolCajero, "")]
    public void Crear_con_datos_invalidos_no_construye_el_objeto(
        string? nombres, string? paterno, string? materno, string? correo, int rolId, string? username)
    {
        var resultado = Usuario.Crear(nombres, paterno, materno, correo, rolId, username, Responsable, Ahora);

        Assert.True(resultado.IsFail);
        Assert.Null(resultado.Value);
        Assert.NotEmpty(resultado.Error);
    }

    [Fact]
    public void Crear_sin_responsable_de_auditoria_falla()
    {
        var resultado = Usuario.Crear(
            "Juan", "Pérez", "Rojas", "juan@correo.com", RolCajero, "jperez", "   ", Ahora);

        Assert.True(resultado.IsFail);
    }

    [Fact]
    public void Crear_rechaza_nombres_mas_largos_que_la_columna()
    {
        var resultado = Usuario.Crear(
            new string('a', Usuario.LongitudMaximaNombres + 1), "Pérez", null,
            "juan@correo.com", RolCajero, "jperez", Responsable, Ahora);

        Assert.True(resultado.IsFail);
    }

    [Fact]
    public void Crear_rechaza_un_apellido_paterno_mas_largo_que_la_columna()
    {
        var resultado = Usuario.Crear(
            "Juan", new string('a', Usuario.LongitudMaximaApellido + 1), null,
            "juan@correo.com", RolCajero, "jperez", Responsable, Ahora);

        Assert.True(resultado.IsFail);
    }

    [Fact]
    public void Crear_rechaza_un_apellido_materno_mas_largo_que_la_columna()
    {
        var resultado = Usuario.Crear(
            "Juan", "Pérez", new string('a', Usuario.LongitudMaximaApellido + 1),
            "juan@correo.com", RolCajero, "jperez", Responsable, Ahora);

        Assert.True(resultado.IsFail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_sin_apellido_materno_lo_deja_vacio_y_no_agrega_espacios(string? materno)
    {
        var resultado = Usuario.Crear(
            "Juan Carlos", "Pérez", materno, "juan@correo.com", RolCajero, "jperez", Responsable, Ahora);

        Assert.True(resultado.IsOk);
        Assert.Null(resultado.Value.ApellidoMaterno);
        Assert.Equal("Pérez", resultado.Value.Apellidos);
        Assert.Equal("Juan Carlos Pérez", resultado.Value.NombreCompleto);
    }

    [Fact]
    public void Crear_compone_apellidos_y_nombre_completo_con_ambos_apellidos()
    {
        var usuario = CrearValido().Value;

        Assert.Equal("Juan Carlos", usuario.Nombres);
        Assert.Equal("Pérez", usuario.ApellidoPaterno);
        Assert.Equal("Rojas", usuario.ApellidoMaterno);
        Assert.Equal("Pérez Rojas", usuario.Apellidos);
    }

    [Fact]
    public void Crear_normaliza_los_espacios_de_cada_campo()
    {
        var resultado = Usuario.Crear(
            "  Juan   Carlos ", "  Pérez ", "  Rojas  ", "juan@correo.com", RolCajero, "jperez", Responsable, Ahora);

        Assert.Equal("Juan Carlos Pérez Rojas", resultado.Value.NombreCompleto);
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
            "Juana", "Pérez", "Soto", "Juana@Correo.com", RolCajero, "ale", despues);

        Assert.True(resultado.IsOk);
        Assert.Equal("Juana Pérez Soto", usuario.NombreCompleto);
        Assert.Equal("juana@correo.com", usuario.Correo);
        Assert.Equal("ale", usuario.UsuarioModificacion);
        Assert.Equal(despues, usuario.FechaModificacion);
    }

    [Fact]
    public void Actualizar_puede_quitar_el_apellido_materno()
    {
        var usuario = CrearValido().Value;

        var resultado = usuario.Actualizar(
            "Juan Carlos", "Pérez", null, "juan@correo.com", RolCajero, "ale", Ahora);

        Assert.True(resultado.IsOk);
        Assert.Null(usuario.ApellidoMaterno);
        Assert.Equal("Juan Carlos Pérez", usuario.NombreCompleto);
    }

    [Fact]
    public void Actualizar_exige_apellido_paterno()
    {
        var usuario = CrearValido().Value;

        var resultado = usuario.Actualizar(
            "Juan Carlos", "  ", "Rojas", "juan@correo.com", RolCajero, "ale", Ahora);

        Assert.True(resultado.IsFail);
        Assert.Equal("Pérez", usuario.ApellidoPaterno);
    }

    [Fact]
    public void Actualizar_con_datos_invalidos_no_modifica_el_usuario()
    {
        var usuario = CrearValido().Value;

        var resultado = usuario.Actualizar("Juana", "Pérez", "Soto", "sin-arroba", RolCajero, "ale", Ahora);

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
            4, "jperez", "juan@correo.com", "Juan", "Pérez", null, RolCajero,
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
            4, "jperez", "juan@correo.com", "Juan", "Pérez", null, RolCajero,
            activo: true, debeCambiarPassword: false, "admin", Ahora, null, null);

        var resultado = usuario.AccesoRestablecido(" ", Ahora);

        Assert.True(resultado.IsFail);
        Assert.False(usuario.DebeCambiarPassword);
    }

    [Fact]
    public void Reconstruir_devuelve_el_usuario_tal_como_esta_en_la_base()
    {
        var usuario = Usuario.Reconstruir(
            4, "jperez", "juan@correo.com", "Juan", "Pérez", null, RolCajero,
            activo: false, debeCambiarPassword: false, "admin", Ahora, "ale", Ahora.AddDays(1));

        Assert.Equal(4, usuario.Id);
        Assert.False(usuario.Activo);
        Assert.False(usuario.DebeCambiarPassword);
        Assert.Equal("ale", usuario.UsuarioModificacion);
        Assert.Equal(Ahora.AddDays(1), usuario.FechaModificacion);
    }
}
