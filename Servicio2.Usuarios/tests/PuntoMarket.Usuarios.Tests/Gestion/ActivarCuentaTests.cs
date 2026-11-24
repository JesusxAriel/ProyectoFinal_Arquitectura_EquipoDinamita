using PuntoMarket.Usuarios.Application.UseCases.Gestion.ActivarCuenta;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class ActivarCuentaTests
{
    private const string TokenDelEnlace = "tok-1";
    private const string PasswordNueva = "NuevaClave#2026";
    private static readonly DateTime Creacion = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Usuario UsuarioDePrueba(bool activo = true, bool debeCambiar = true) =>
        Usuario.Reconstruir(
            7, "aperez", "ana@correo.com", "Ana Pérez", 2,
            activo, debeCambiar, "admin", Creacion, null, null);

    private sealed record Escenario(
        ActivarCuentaCasoDeUso Caso,
        UsuarioRepositorioStub Repositorio,
        GestorDeIdentidadFalso Identidad,
        ActivacionRepositorioFalso Activaciones,
        RelojFijo Reloj);

    private static Escenario Preparar(Usuario? usuario = null, int minutosDeVigencia = 60)
    {
        usuario ??= UsuarioDePrueba();
        var repositorio = new UsuarioRepositorioStub { UsuarioEncontrado = usuario };
        var identidad = new GestorDeIdentidadFalso();
        var activaciones = new ActivacionRepositorioFalso();
        var reloj = new RelojFijo();
        activaciones.Usuarios[usuario.Id] = usuario;
        activaciones.Tokens[usuario.Id] = ("hash:" + TokenDelEnlace, reloj.Ahora.AddMinutes(minutosDeVigencia));

        return new Escenario(
            new ActivarCuentaCasoDeUso(activaciones, repositorio, identidad, new GeneradorTokenFalso(), reloj),
            repositorio, identidad, activaciones, reloj);
    }

    private static ActivarCuentaEntrada Entrada(
        string? token = TokenDelEnlace, string? nueva = PasswordNueva, string? confirmacion = PasswordNueva) =>
        new(token, nueva, confirmacion);

    [Fact]
    public async Task Activa_fija_la_contrasena_en_identidad_y_deja_de_exigir_el_cambio()
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsOk);
        Assert.True(resultado.Value.Activada);
        Assert.Equal(("aperez", PasswordNueva), Assert.Single(escenario.Identidad.Cambios));

        var guardado = Assert.Single(escenario.Repositorio.Actualizados);
        Assert.False(guardado.DebeCambiarPassword);
        Assert.True(guardado.Activo);
        Assert.Equal("aperez", guardado.UsuarioModificacion);
        Assert.Equal(escenario.Reloj.Ahora, guardado.FechaModificacion);
    }

    [Fact]
    public async Task El_token_es_de_un_solo_uso()
    {
        var escenario = Preparar();

        var primero = await escenario.Caso.EjecutarAsync(Entrada());
        var segundo = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(primero.IsOk);
        Assert.True(segundo.IsFail);
        Assert.Equal(ActivarCuentaCasoDeUso.EnlaceInvalido, segundo.Error);
        Assert.Single(escenario.Identidad.Cambios);
        Assert.Empty(escenario.Activaciones.Tokens);
    }

    [Fact]
    public async Task Un_token_con_espacios_alrededor_se_normaliza()
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(Entrada(token: $"  {TokenDelEnlace}  "));

        Assert.True(resultado.IsOk);
    }

    [Fact]
    public async Task Un_token_vencido_se_rechaza_sin_cambiar_nada()
    {
        var escenario = Preparar(minutosDeVigencia: -1);

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsFail);
        Assert.Equal(ActivarCuentaCasoDeUso.EnlaceInvalido, resultado.Error);
        Assert.Empty(escenario.Identidad.Cambios);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Fact]
    public async Task Un_usuario_inactivo_no_puede_activar_con_un_token_antiguo()
    {
        var escenario = Preparar(UsuarioDePrueba(activo: false));

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsFail);
        Assert.Equal(ActivarCuentaCasoDeUso.EnlaceInvalido, resultado.Error);
        Assert.Empty(escenario.Identidad.Cambios);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Theory]
    [InlineData("token-desconocido")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Un_token_desconocido_o_vacio_da_el_mismo_mensaje_generico(string? token)
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(Entrada(token: token));

        Assert.True(resultado.IsFail);
        Assert.Equal(ActivarCuentaCasoDeUso.EnlaceInvalido, resultado.Error);
        Assert.Empty(escenario.Identidad.Cambios);
    }

    [Fact]
    public async Task Invalido_vencido_y_usado_comparten_exactamente_el_mismo_mensaje()
    {
        var invalido = await Preparar().Caso.EjecutarAsync(Entrada(token: "otro"));
        var vencido = await Preparar(minutosDeVigencia: -5).Caso.EjecutarAsync(Entrada());
        var usado = Preparar();
        await usado.Caso.EjecutarAsync(Entrada());
        var reutilizado = await usado.Caso.EjecutarAsync(Entrada());

        Assert.Equal(invalido.Error, vencido.Error);
        Assert.Equal(invalido.Error, reutilizado.Error);
    }

    [Fact]
    public async Task Una_confirmacion_distinta_se_rechaza_sin_consumir_el_token()
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(Entrada(confirmacion: "OtraClave#2026"));

        Assert.True(resultado.IsFail);
        Assert.Equal("La confirmación no coincide con la contraseña nueva.", resultado.Error);
        Assert.Single(escenario.Activaciones.Tokens);
        Assert.Empty(escenario.Identidad.Cambios);
    }

    [Theory]
    [InlineData("corta1!")]
    [InlineData("sinmayuscula1!")]
    [InlineData("SINMINUSCULA1!")]
    [InlineData("SinNumero!!")]
    [InlineData("SinEspecial123")]
    public async Task Una_contrasena_que_incumple_la_politica_no_consume_el_token(string password)
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(Entrada(nueva: password, confirmacion: password));

        Assert.True(resultado.IsFail);
        Assert.Single(escenario.Activaciones.Tokens);
        Assert.Empty(escenario.Identidad.Cambios);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Fact]
    public async Task Si_identidad_falla_restaura_el_token_y_no_actualiza_al_usuario()
    {
        var escenario = Preparar();
        escenario.Identidad.ResultadoCambiarPassword = Result.Fail("El servicio de identidad no está disponible.");

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsFail);
        Assert.Equal(ActivarCuentaCasoDeUso.ActivacionNoCompletada, resultado.Error);
        Assert.Equal("hash:" + TokenDelEnlace, Assert.Single(escenario.Activaciones.Restaurados).Hash);
        Assert.Single(escenario.Activaciones.Tokens);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Fact]
    public async Task Si_no_se_puede_guardar_al_usuario_restaura_el_token()
    {
        var escenario = Preparar();
        escenario.Repositorio.ResultadoActualizar = Result<Usuario>.Fail("El usuario ya no existe.");

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsFail);
        Assert.Equal(ActivarCuentaCasoDeUso.ActivacionNoCompletada, resultado.Error);
        Assert.Single(escenario.Activaciones.Restaurados);
        Assert.Single(escenario.Activaciones.Tokens);
    }

    [Fact]
    public async Task Los_mensajes_de_error_nunca_contienen_el_token_ni_la_contrasena()
    {
        var resultados = new List<Result<ActivarCuentaSalida>>
        {
            await Preparar(minutosDeVigencia: -1).Caso.EjecutarAsync(Entrada()),
            await Preparar().Caso.EjecutarAsync(Entrada(confirmacion: "x")),
            await Preparar().Caso.EjecutarAsync(Entrada(nueva: "debil", confirmacion: "debil"))
        };
        var conFalloDeIdentidad = Preparar();
        conFalloDeIdentidad.Identidad.ResultadoCambiarPassword = Result.Fail("Falla.");
        resultados.Add(await conFalloDeIdentidad.Caso.EjecutarAsync(Entrada()));

        foreach (var resultado in resultados)
        {
            Assert.DoesNotContain(TokenDelEnlace, resultado.Error);
            Assert.DoesNotContain(PasswordNueva, resultado.Error);
        }
    }
}
