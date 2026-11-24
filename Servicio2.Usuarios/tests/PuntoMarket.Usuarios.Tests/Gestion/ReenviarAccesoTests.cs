using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ReenviarAcceso;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class ReenviarAccesoTests
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(60);
    private const int RolCajero = 2;
    private const string Administrador = "admin";
    private static readonly DateTime Creacion = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Usuario UsuarioDePrueba(bool activo = true, bool debeCambiar = false) =>
        Usuario.Reconstruir(
            7, "aperez", "ana@correo.com", "Ana Pérez", RolCajero,
            activo, debeCambiar, Administrador, Creacion, null, null);

    private sealed record Escenario(
        ReenviarAccesoCasoDeUso Caso,
        UsuarioRepositorioStub Repositorio,
        GestorDeIdentidadFalso Identidad,
        EnviadorCorreoSpy Correo,
        GeneradorPasswordFalso Password,
        GeneradorTokenFalso Token,
        ActivacionRepositorioFalso Activaciones,
        RelojFijo Reloj);

    private static Escenario Preparar(Usuario? usuario)
    {
        var repositorio = new UsuarioRepositorioStub { UsuarioEncontrado = usuario };
        var identidad = new GestorDeIdentidadFalso();
        var correo = new EnviadorCorreoSpy();
        var password = new GeneradorPasswordFalso();
        var token = new GeneradorTokenFalso();
        var activaciones = new ActivacionRepositorioFalso();
        var reloj = new RelojFijo();

        return new Escenario(
            new ReenviarAccesoCasoDeUso(
                repositorio, identidad, correo, password, token, activaciones,
                new ConfiguracionActivacion(Vigencia), new UsuarioActualStub(Administrador), reloj),
            repositorio, identidad, correo, password, token, activaciones, reloj);
    }

    [Fact]
    public async Task Fija_una_contrasena_desconocida_marca_el_cambio_obligatorio_y_envia_el_enlace()
    {
        var escenario = Preparar(UsuarioDePrueba());

        var resultado = await escenario.Caso.EjecutarAsync(new ReenviarAccesoEntrada(7));

        Assert.True(resultado.IsOk);
        Assert.True(resultado.Value.CorreoEnviado);
        Assert.Equal(("aperez", escenario.Password.Password), Assert.Single(escenario.Identidad.Cambios));

        var guardado = Assert.Single(escenario.Repositorio.Actualizados);
        Assert.True(guardado.DebeCambiarPassword);
        Assert.Equal(Administrador, guardado.UsuarioModificacion);
        Assert.Equal(escenario.Reloj.Ahora, guardado.FechaModificacion);

        var envio = Assert.Single(escenario.Correo.Envios);
        Assert.Equal(("ana@correo.com", "aperez", "token-1", Vigencia), envio);
    }

    [Fact]
    public async Task Un_usuario_inexistente_devuelve_el_error_de_no_encontrado_sin_efectos()
    {
        var escenario = Preparar(null);

        var resultado = await escenario.Caso.EjecutarAsync(new ReenviarAccesoEntrada(99));

        Assert.True(resultado.IsFail);
        Assert.Equal(MensajesDeGestion.UsuarioNoEncontrado, resultado.Error);
        Assert.Empty(escenario.Identidad.Cambios);
        Assert.Empty(escenario.Repositorio.Actualizados);
        Assert.Empty(escenario.Correo.Envios);
        Assert.Equal(0, escenario.Password.Llamadas);
    }

    [Fact]
    public async Task Un_usuario_inactivo_se_rechaza_en_espanol_sin_efectos()
    {
        var escenario = Preparar(UsuarioDePrueba(activo: false));

        var resultado = await escenario.Caso.EjecutarAsync(new ReenviarAccesoEntrada(7));

        Assert.True(resultado.IsFail);
        Assert.Equal(ReenviarAccesoCasoDeUso.UsuarioInactivo, resultado.Error);
        Assert.Empty(escenario.Identidad.Cambios);
        Assert.Empty(escenario.Repositorio.Actualizados);
        Assert.Empty(escenario.Correo.Envios);
        Assert.Equal(0, escenario.Password.Llamadas);
    }

    [Fact]
    public async Task Si_el_correo_falla_igual_tiene_exito_con_correo_enviado_en_falso()
    {
        var escenario = Preparar(UsuarioDePrueba());
        escenario.Correo.Resultado = Result.Fail("Sin servidor SMTP.");

        var resultado = await escenario.Caso.EjecutarAsync(new ReenviarAccesoEntrada(7));

        Assert.True(resultado.IsOk);
        Assert.False(resultado.Value.CorreoEnviado);
        Assert.Single(escenario.Identidad.Cambios);
        Assert.True(Assert.Single(escenario.Repositorio.Actualizados).DebeCambiarPassword);
    }

    [Fact]
    public async Task Si_el_envio_lanza_una_excepcion_de_configuracion_tambien_responde_correo_enviado_en_falso()
    {
        var escenario = Preparar(UsuarioDePrueba());
        escenario.Correo.ExcepcionAlEnviar = new InvalidOperationException("SMTP sin configurar");

        var resultado = await escenario.Caso.EjecutarAsync(new ReenviarAccesoEntrada(7));

        Assert.True(resultado.IsOk);
        Assert.False(resultado.Value.CorreoEnviado);
    }

    [Fact]
    public async Task Si_la_identidad_falla_no_se_guarda_ni_se_envia_correo()
    {
        var escenario = Preparar(UsuarioDePrueba());
        escenario.Identidad.ResultadoCambiarPassword = Result.Fail("El servicio de identidad no está disponible.");

        var resultado = await escenario.Caso.EjecutarAsync(new ReenviarAccesoEntrada(7));

        Assert.True(resultado.IsFail);
        Assert.Equal("El servicio de identidad no está disponible.", resultado.Error);
        Assert.Empty(escenario.Repositorio.Actualizados);
        Assert.Empty(escenario.Correo.Envios);
    }

    [Fact]
    public async Task Genera_un_token_nuevo_que_invalida_el_anterior()
    {
        var escenario = Preparar(UsuarioDePrueba());
        escenario.Activaciones.Usuarios[7] = UsuarioDePrueba();
        escenario.Activaciones.Tokens[7] = ("hash:token-viejo", escenario.Reloj.Ahora.AddMinutes(30));

        var resultado = await escenario.Caso.EjecutarAsync(new ReenviarAccesoEntrada(7));

        Assert.True(resultado.IsOk);
        Assert.Equal("hash:token-1", escenario.Activaciones.Tokens[7].Hash);
        Assert.Equal(escenario.Reloj.Ahora + Vigencia, escenario.Activaciones.Tokens[7].ExpiraUtc);
        Assert.Null(await escenario.Activaciones.ConsumirToken("hash:token-viejo", escenario.Reloj.Ahora));
        Assert.NotNull(await escenario.Activaciones.ConsumirToken("hash:token-1", escenario.Reloj.Ahora));
    }

    [Fact]
    public async Task Si_no_se_puede_guardar_el_token_devuelve_el_error_y_no_envia_correo()
    {
        var escenario = Preparar(UsuarioDePrueba());
        escenario.Activaciones.ResultadoGuardar = Result.Fail("El usuario ya no existe.");

        var resultado = await escenario.Caso.EjecutarAsync(new ReenviarAccesoEntrada(7));

        Assert.True(resultado.IsFail);
        Assert.Empty(escenario.Correo.Envios);
    }

    [Fact]
    public async Task La_password_temporal_nunca_aparece_en_el_resultado()
    {
        var escenario = Preparar(UsuarioDePrueba());

        var resultado = await escenario.Caso.EjecutarAsync(new ReenviarAccesoEntrada(7));

        Assert.DoesNotContain(escenario.Password.Password, resultado.Value.ToString());
        Assert.DoesNotContain(escenario.Password.Password, resultado.Error);
    }
}
