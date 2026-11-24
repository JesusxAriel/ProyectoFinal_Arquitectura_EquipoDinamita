using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CrearUsuario;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Infrastructure.Identity;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class CrearUsuarioTests
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(60);
    private const int RolCajero = 2;
    private const string Administrador = "admin";

    private static CrearUsuarioEntrada Entrada(
        string? nombres = "Juan Carlos",
        string? apellidos = "Pérez Rojas",
        string? correo = "Juan@Correo.com",
        int rolId = RolCajero) =>
        new(nombres, apellidos, correo, rolId);

    private sealed record Escenario(
        CrearUsuarioCasoDeUso Caso,
        UsuarioRepositorioStub Repositorio,
        GestorDeIdentidadFalso Identidad,
        EnviadorCorreoSpy Correo,
        GeneradorPasswordFalso Password,
        GeneradorTokenFalso Token,
        ActivacionRepositorioFalso Activaciones,
        RelojFijo Reloj);

    private static Escenario Preparar(bool rolActivo = true, Rol? rol = null)
    {
        var repositorio = new UsuarioRepositorioStub
        {
            RolEncontrado = rol ?? Rol.Reconstruir(RolCajero, "Cajero", rolActivo)
        };
        var identidad = new GestorDeIdentidadFalso();
        var correo = new EnviadorCorreoSpy();
        var password = new GeneradorPasswordFalso();
        var token = new GeneradorTokenFalso();
        var activaciones = new ActivacionRepositorioFalso();
        var reloj = new RelojFijo();

        return new Escenario(
            new CrearUsuarioCasoDeUso(
                repositorio, identidad, correo, password, token, activaciones,
                new ConfiguracionActivacion(Vigencia), new UsuarioActualStub(Administrador), reloj),
            repositorio, identidad, correo, password, token, activaciones, reloj);
    }

    [Fact]
    public async Task Crea_el_usuario_con_el_protocolo_lo_registra_en_keycloak_y_envia_el_enlace_de_activacion()
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsOk);
        Assert.Equal(escenario.Repositorio.IdAsignado, resultado.Value.Id);
        Assert.Equal("jperez", resultado.Value.Username);
        Assert.True(resultado.Value.CorreoEnviado);

        var guardado = Assert.Single(escenario.Repositorio.Guardados);
        Assert.Equal("jperez", guardado.Username);
        Assert.Equal("juan@correo.com", guardado.Correo);
        Assert.Equal("Juan Carlos Pérez Rojas", guardado.NombreCompleto);
        Assert.True(guardado.DebeCambiarPassword);
        Assert.True(guardado.Activo);
        Assert.Equal(Administrador, guardado.UsuarioCreacion);
        Assert.Equal(escenario.Reloj.Ahora, guardado.FechaCreacion);

        Assert.Equal([nameof(GestorDeIdentidadFalso.CrearUsuario)], escenario.Identidad.Llamadas);
        Assert.Equal(
            ("juan@correo.com", "jperez", "token-1", Vigencia),
            Assert.Single(escenario.Correo.Envios));
    }

    [Fact]
    public async Task Numera_el_username_cuando_el_protocolo_choca_con_los_existentes()
    {
        var escenario = Preparar();
        escenario.Repositorio.UsernamesExistentes.Add("jperez");
        escenario.Repositorio.UsernamesExistentes.Add("jperez2");

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsOk);
        Assert.Equal("jperez3", resultado.Value.Username);
        Assert.Equal(["jperez", "jperez2", "jperez3"], escenario.Repositorio.UsernamesVerificados);
    }

    [Fact]
    public async Task La_contrasena_desconocida_se_genera_una_vez_y_nunca_viaja_al_correo()
    {
        var escenario = Preparar();

        await escenario.Caso.EjecutarAsync(Entrada());

        Assert.Equal(1, escenario.Password.Llamadas);
        var envio = Assert.Single(escenario.Correo.Envios);
        Assert.DoesNotContain(escenario.Password.Password, envio.Correo + envio.Username + envio.Token);
    }

    [Fact]
    public async Task Guarda_solo_el_hash_del_token_con_el_vencimiento_configurado()
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        var guardado = Assert.Single(escenario.Activaciones.Guardados);
        Assert.Equal(resultado.Value.Id, guardado.UsuarioId);
        Assert.Equal("hash:token-1", guardado.Hash);
        Assert.Equal(escenario.Reloj.Ahora + Vigencia, guardado.ExpiraUtc);
    }

    [Fact]
    public async Task Si_el_correo_falla_responde_ok_con_correo_enviado_en_falso_y_conserva_el_token()
    {
        var escenario = Preparar();
        escenario.Correo.Resultado = Result.Fail("Sin servidor SMTP.");

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsOk);
        Assert.False(resultado.Value.CorreoEnviado);
        Assert.Single(escenario.Activaciones.Guardados);
    }

    [Fact]
    public async Task Si_no_se_puede_guardar_el_token_no_envia_correo_y_responde_correo_enviado_en_falso()
    {
        var escenario = Preparar();
        escenario.Activaciones.ResultadoGuardar = Result.Fail("El usuario ya no existe.");

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsOk);
        Assert.False(resultado.Value.CorreoEnviado);
        Assert.Empty(escenario.Correo.Envios);
    }

    [Fact]
    public async Task Rechaza_un_rol_que_no_existe()
    {
        var escenario = Preparar();
        escenario.Repositorio.RolEncontrado = null;

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsFail);
        Assert.Equal(MensajesDeGestion.RolNoDisponible, resultado.Error);
        Assert.Empty(escenario.Repositorio.Guardados);
        Assert.Empty(escenario.Identidad.Llamadas);
    }

    [Fact]
    public async Task Rechaza_un_rol_inactivo()
    {
        var escenario = Preparar(rolActivo: false);

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsFail);
        Assert.Equal(MensajesDeGestion.RolNoDisponible, resultado.Error);
        Assert.Empty(escenario.Repositorio.Guardados);
    }

    [Fact]
    public async Task Rechaza_un_correo_que_ya_esta_en_uso()
    {
        var escenario = Preparar();
        escenario.Repositorio.CorreoExistente = true;

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsFail);
        Assert.Equal(CrearUsuarioCasoDeUso.CorreoEnUso, resultado.Error);
        Assert.Empty(escenario.Repositorio.Guardados);
    }

    [Theory]
    [InlineData("Juan", "Pérez", "sin-arroba")]
    [InlineData("Juan", "Pérez", null)]
    [InlineData("", "Pérez", "juan@correo.com")]
    [InlineData("123", "456", "juan@correo.com")]
    public async Task No_guarda_nada_si_el_dominio_rechaza_los_datos(
        string? nombres, string? apellidos, string? correo)
    {
        var escenario = Preparar();

        var resultado = await escenario.Caso.EjecutarAsync(Entrada(nombres, apellidos, correo));

        Assert.True(resultado.IsFail);
        Assert.NotEmpty(resultado.Error);
        Assert.Empty(escenario.Repositorio.Guardados);
        Assert.Empty(escenario.Identidad.Llamadas);
    }

    [Fact]
    public async Task No_llama_a_keycloak_si_la_base_de_datos_rechaza_el_alta()
    {
        var escenario = Preparar();
        escenario.Repositorio.ResultadoGuardar = Result<Usuario>.Fail("Ya existe un usuario con ese correo.");

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsFail);
        Assert.Equal("Ya existe un usuario con ese correo.", resultado.Error);
        Assert.Empty(escenario.Identidad.Llamadas);
        Assert.Empty(escenario.Correo.Envios);
    }

    [Fact]
    public async Task Si_falla_keycloak_deja_el_usuario_inactivo_y_avisa_sin_enviar_correo()
    {
        var escenario = Preparar();
        escenario.Identidad.ResultadoCrearUsuario = Result.Fail(KeycloakMensajes.ServicioNoDisponible);

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsFail);
        Assert.Contains(KeycloakMensajes.ServicioNoDisponible, resultado.Error);
        Assert.Contains(CrearUsuarioCasoDeUso.IdentidadFallida, resultado.Error);

        var desactivado = Assert.Single(escenario.Repositorio.Actualizados);
        Assert.False(desactivado.Activo);
        Assert.Equal(escenario.Repositorio.IdAsignado, desactivado.Id);
        Assert.Equal(Administrador, desactivado.UsuarioModificacion);
        Assert.Empty(escenario.Correo.Envios);
    }

    [Fact]
    public async Task Si_falla_el_correo_el_usuario_queda_creado_y_activo_pero_lo_informa()
    {
        var escenario = Preparar();
        escenario.Correo.Resultado = Result.Fail("No se pudo enviar el correo.");

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsOk);
        Assert.False(resultado.Value.CorreoEnviado);
        Assert.Equal(escenario.Repositorio.IdAsignado, resultado.Value.Id);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }

    [Fact]
    public async Task Un_fallo_tecnico_del_correo_no_tumba_el_alta_ya_realizada()
    {
        var escenario = Preparar();
        escenario.Correo.ExcepcionAlEnviar = new InvalidOperationException(
            "La configuración SMTP requiere Host, FromAddress y un Port válido.");

        var resultado = await escenario.Caso.EjecutarAsync(Entrada());

        Assert.True(resultado.IsOk);
        Assert.False(resultado.Value.CorreoEnviado);
        Assert.Single(escenario.Repositorio.Guardados);
        Assert.Empty(escenario.Repositorio.Actualizados);
    }
}
