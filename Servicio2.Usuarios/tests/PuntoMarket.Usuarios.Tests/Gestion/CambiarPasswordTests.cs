using PuntoMarket.Usuarios.Application.UseCases.Gestion.CambiarPassword;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Infrastructure.Identity;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class CambiarPasswordTests
{
    private const string Username = "aperez";
    private const string Actual = "Temporal#1";
    private const string Nueva = "Segura#2026";
    private static readonly DateTime Creacion = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Usuario UsuarioDePrueba(bool activo = true) =>
        Usuario.Reconstruir(
            5, Username, "ana@correo.com", "Ana", "Pérez", null, 2,
            activo, debeCambiarPassword: true, "admin", Creacion, null, null);

    private sealed record Escenario(
        CambiarPasswordCasoDeUso Caso,
        UsuarioRepositorioStub Repositorio,
        GestorDeIdentidadFalso Identidad,
        RelojFijo Reloj);

    private static Escenario Preparar(Usuario? usuario)
    {
        var repositorio = new UsuarioRepositorioStub { UsuarioEncontrado = usuario };
        var identidad = new GestorDeIdentidadFalso();
        var reloj = new RelojFijo();

        return new Escenario(
            new CambiarPasswordCasoDeUso(repositorio, identidad, new UsuarioActualStub(Username), reloj),
            repositorio,
            identidad,
            reloj);
    }

    [Fact]
    public async Task Cambia_en_keycloak_y_despues_marca_la_tabla()
    {
        var escenario = Preparar(UsuarioDePrueba());

        var resultado = await escenario.Caso.EjecutarAsync(new(Actual, Nueva, Nueva));

        Assert.True(resultado.IsOk);
        Assert.Equal(
            [nameof(GestorDeIdentidadFalso.VerificarPassword), nameof(GestorDeIdentidadFalso.CambiarPassword)],
            escenario.Identidad.Llamadas);
        Assert.Equal((Username, Actual), Assert.Single(escenario.Identidad.Verificaciones));
        Assert.Equal((Username, Nueva), Assert.Single(escenario.Identidad.Cambios));

        var guardado = Assert.Single(escenario.Repositorio.Actualizados);
        Assert.False(guardado.DebeCambiarPassword);
        Assert.Equal(Username, guardado.UsuarioModificacion);
        Assert.Equal(escenario.Reloj.Ahora, guardado.FechaModificacion);
    }

    [Theory]
    [InlineData("", Nueva, Nueva)]
    [InlineData("   ", Nueva, Nueva)]
    [InlineData(null, Nueva, Nueva)]
    [InlineData(Actual, Nueva, "Otra#2026")]
    [InlineData(Actual, Nueva, null)]
    [InlineData(Actual, "corta", "corta")]
    [InlineData(Actual, "sinmayuscula1!", "sinmayuscula1!")]
    [InlineData(Actual, Actual, Actual)]
    public async Task No_toca_keycloak_ni_la_tabla_si_los_datos_no_pasan_el_dominio(
        string? actual, string? nueva, string? confirmacion)
    {
        var escenario = Preparar(UsuarioDePrueba());

        var resultado = await escenario.Caso.EjecutarAsync(new(actual, nueva, confirmacion));

        Assert.True(resultado.IsFail);
        Assert.NotEmpty(resultado.Error);
        Assert.Empty(escenario.Identidad.Llamadas);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Fact]
    public async Task Rechaza_una_contrasena_nueva_igual_a_la_actual_con_su_propio_mensaje()
    {
        var escenario = Preparar(UsuarioDePrueba());

        var resultado = await escenario.Caso.EjecutarAsync(new(Actual, Actual, Actual));

        Assert.Equal(CambiarPasswordCasoDeUso.PasswordRepetida, resultado.Error);
    }

    [Fact]
    public async Task Falla_cuando_el_usuario_del_token_no_esta_en_la_tabla()
    {
        var escenario = Preparar(null);

        var resultado = await escenario.Caso.EjecutarAsync(new(Actual, Nueva, Nueva));

        Assert.True(resultado.IsFail);
        Assert.Equal(CambiarPasswordCasoDeUso.UsuarioNoRegistrado, resultado.Error);
        Assert.Empty(escenario.Identidad.Llamadas);
    }

    [Fact]
    public async Task Falla_cuando_el_usuario_esta_inactivo()
    {
        var escenario = Preparar(UsuarioDePrueba(activo: false));

        var resultado = await escenario.Caso.EjecutarAsync(new(Actual, Nueva, Nueva));

        Assert.True(resultado.IsFail);
        Assert.Equal(CambiarPasswordCasoDeUso.UsuarioInactivo, resultado.Error);
        Assert.Empty(escenario.Identidad.Llamadas);
    }

    [Fact]
    public async Task Propaga_el_mensaje_de_keycloak_cuando_la_actual_es_incorrecta()
    {
        var escenario = Preparar(UsuarioDePrueba());
        escenario.Identidad.ResultadoVerificarPassword =
            Result.Fail(KeycloakMensajes.PasswordActualIncorrecta);

        var resultado = await escenario.Caso.EjecutarAsync(new(Actual, Nueva, Nueva));

        Assert.True(resultado.IsFail);
        Assert.Equal(KeycloakMensajes.PasswordActualIncorrecta, resultado.Error);
        Assert.Equal([nameof(GestorDeIdentidadFalso.VerificarPassword)], escenario.Identidad.Llamadas);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Fact]
    public async Task No_marca_la_tabla_si_keycloak_no_pudo_cambiar_la_contrasena()
    {
        var escenario = Preparar(UsuarioDePrueba());
        escenario.Identidad.ResultadoCambiarPassword =
            Result.Fail(KeycloakMensajes.ServicioNoDisponible);

        var resultado = await escenario.Caso.EjecutarAsync(new(Actual, Nueva, Nueva));

        Assert.True(resultado.IsFail);
        Assert.Equal(KeycloakMensajes.ServicioNoDisponible, resultado.Error);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Fact]
    public async Task Informa_el_fallo_si_no_se_pudo_guardar_la_marca_en_la_tabla()
    {
        var escenario = Preparar(UsuarioDePrueba());
        escenario.Repositorio.ResultadoActualizar =
            Result<Usuario>.Fail("El usuario ya no existe.");

        var resultado = await escenario.Caso.EjecutarAsync(new(Actual, Nueva, Nueva));

        Assert.True(resultado.IsFail);
        Assert.Equal("El usuario ya no existe.", resultado.Error);
        Assert.Single(escenario.Identidad.Cambios);
    }
}
