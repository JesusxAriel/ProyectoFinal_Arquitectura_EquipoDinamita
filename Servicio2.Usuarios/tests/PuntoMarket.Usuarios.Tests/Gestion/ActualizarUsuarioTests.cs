using PuntoMarket.Usuarios.Application.UseCases.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ActualizarUsuario;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class ActualizarUsuarioTests
{
    private const int RolCajero = 2;
    private const string Administrador = "admin";
    private static readonly DateTime Creacion = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Usuario UsuarioDePrueba() =>
        Usuario.Reconstruir(
            7, "aperez", "ana@correo.com", "Ana Pérez", RolCajero,
            activo: true, debeCambiarPassword: false, Administrador, Creacion, null, null);

    private sealed record Escenario(
        ActualizarUsuarioCasoDeUso Caso,
        UsuarioRepositorioStub Repositorio,
        RelojFijo Reloj);

    private static Escenario Preparar(Usuario? usuario = null, bool rolActivo = true)
    {
        var repositorio = new UsuarioRepositorioStub
        {
            UsuarioEncontrado = usuario ?? UsuarioDePrueba(),
            RolEncontrado = Rol.Reconstruir(RolCajero, "Cajero", rolActivo)
        };
        var reloj = new RelojFijo();

        return new Escenario(
            new ActualizarUsuarioCasoDeUso(repositorio, new UsuarioActualStub(Administrador), reloj),
            repositorio,
            reloj);
    }

    [Fact]
    public async Task Actualiza_los_datos_y_registra_quien_y_cuando()
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(
            new(7, "Ana María", "Pérez Soto", "Ana.Maria@Correo.com", RolCajero));

        Assert.True(resultado.IsOk);
        Assert.Equal("Ana María Pérez Soto", resultado.Value.Usuario.NombreCompleto);
        Assert.Equal("ana.maria@correo.com", resultado.Value.Usuario.Correo);
        Assert.Equal("Cajero", resultado.Value.Usuario.Rol);

        var guardado = Assert.Single(escenario.Repositorio.Actualizados);
        Assert.Equal(Administrador, guardado.UsuarioModificacion);
        Assert.Equal(escenario.Reloj.Ahora, guardado.FechaModificacion);
    }

    [Fact]
    public async Task No_cambia_el_username_ni_el_cambio_obligatorio_de_contrasena()
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(
            new(7, "Ana", "Pérez", "otra@correo.com", RolCajero));

        Assert.Equal("aperez", resultado.Value.Usuario.Username);
        Assert.False(resultado.Value.Usuario.DebeCambiarPassword);
    }

    [Fact]
    public async Task Rechaza_un_rol_inactivo_sin_leer_al_usuario()
    {
        var escenario = Preparar(rolActivo: false);

        var resultado = await escenario.Caso.EjecutarAsync(new(7, "Ana", "Pérez", "ana@correo.com", RolCajero));

        Assert.True(resultado.IsFail);
        Assert.Equal(MensajesDeGestion.RolNoDisponible, resultado.Error);
        Assert.Empty(escenario.Repositorio.IdsConsultados);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Fact]
    public async Task Falla_cuando_el_usuario_no_existe()
    {
        var escenario = Preparar();
        escenario.Repositorio.UsuarioEncontrado = null;

        var resultado = await escenario.Caso.EjecutarAsync(new(99, "Ana", "Pérez", "ana@correo.com", RolCajero));

        Assert.True(resultado.IsFail);
        Assert.Equal(MensajesDeGestion.UsuarioNoEncontrado, resultado.Error);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Theory]
    [InlineData("Ana", "Pérez", "sin-arroba")]
    [InlineData("", "Pérez", "ana@correo.com")]
    [InlineData("Ana", null, "ana@correo.com")]
    public async Task No_guarda_nada_si_el_dominio_rechaza_los_datos(
        string? nombres, string? apellidos, string? correo)
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(new(7, nombres, apellidos, correo, RolCajero));

        Assert.True(resultado.IsFail);
        Assert.NotEmpty(resultado.Error);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Fact]
    public async Task Propaga_el_mensaje_del_repositorio_si_el_correo_queda_duplicado()
    {
        var escenario = Preparar();
        escenario.Repositorio.ResultadoActualizar =
            Result<Usuario>.Fail("Ya existe un usuario con ese correo.");

        var resultado = await escenario.Caso.EjecutarAsync(
            new(7, "Ana", "Pérez", "repetido@correo.com", RolCajero));

        Assert.True(resultado.IsFail);
        Assert.Equal("Ya existe un usuario con ese correo.", resultado.Error);
    }
}
