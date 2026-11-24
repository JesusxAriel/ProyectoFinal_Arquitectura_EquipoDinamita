using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Domain.Services;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.CrearUsuario;

public sealed class CrearUsuarioCasoDeUso(
    IUsuarioRepositorio repositorio,
    IGestorDeIdentidad gestorDeIdentidad,
    IEnviadorCorreo enviadorCorreo,
    IGeneradorPassword generadorPassword,
    IUsuarioActual usuarioActual,
    IReloj reloj)
{
    public const string CorreoEnUso = "Ya existe un usuario con ese correo.";
    public const string SinUsernameDisponible = "No se pudo generar un nombre de usuario disponible.";
    public const string IdentidadFallida =
        "El usuario quedó inactivo porque no se pudo crear en el sistema de identidad.";

    private const int MaximoIntentosDeUsername = 100;

    public async Task<Result<CrearUsuarioSalida>> EjecutarAsync(
        CrearUsuarioEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var rol = await repositorio.ObtenerRol(entrada.RolId, cancellationToken);
        if (rol is null || !rol.Activo)
            return Result<CrearUsuarioSalida>.Fail(MensajesDeGestion.RolNoDisponible);

        var nombreBase = GeneradorNombreDeUsuario.GenerarBase(entrada.Nombres, entrada.ApellidoPaterno);
        if (nombreBase.IsFail)
            return Result<CrearUsuarioSalida>.Fail(nombreBase.Error);

        var username = await BuscarUsernameLibre(nombreBase.Value, cancellationToken);
        if (username is null)
            return Result<CrearUsuarioSalida>.Fail(SinUsernameDisponible);

        var creacion = Usuario.Crear(
            entrada.Nombres,
            entrada.ApellidoPaterno,
            entrada.ApellidoMaterno,
            entrada.Correo,
            entrada.RolId,
            username,
            usuarioActual.Username,
            reloj.Ahora);

        if (creacion.IsFail)
            return Result<CrearUsuarioSalida>.Fail(creacion.Error);

        var usuario = creacion.Value;
        if (await repositorio.ExisteCorreo(usuario.Correo, cancellationToken))
            return Result<CrearUsuarioSalida>.Fail(CorreoEnUso);

        var passwordTemporal = generadorPassword.Generar();

        var guardado = await repositorio.Guardar(usuario, cancellationToken);
        if (guardado.IsFail)
            return Result<CrearUsuarioSalida>.Fail(guardado.Error);

        var guardadoConId = guardado.Value;
        var identidad = await gestorDeIdentidad.CrearUsuario(
            guardadoConId.Username,
            guardadoConId.Correo,
            guardadoConId.Nombres,
            guardadoConId.Apellidos,
            rol.Nombre,
            passwordTemporal);

        if (identidad.IsFail)
        {
            await Desactivar(guardadoConId, cancellationToken);
            return Result<CrearUsuarioSalida>.Fail($"{identidad.Error} {IdentidadFallida}");
        }

        var correoEnviado = await EnviarCredenciales(guardadoConId, passwordTemporal, cancellationToken);

        return Result<CrearUsuarioSalida>.Ok(
            new CrearUsuarioSalida(guardadoConId.Id, guardadoConId.Username, correoEnviado));
    }

    private async Task<string?> BuscarUsernameLibre(string nombreBase, CancellationToken cancellationToken)
    {
        for (var intento = GeneradorNombreDeUsuario.PrimerIntento;
             intento <= MaximoIntentosDeUsername;
             intento++)
        {
            var candidato = GeneradorNombreDeUsuario.Siguiente(nombreBase, intento);
            if (!await repositorio.ExisteUsername(candidato, cancellationToken))
                return candidato;
        }

        return null;
    }

    private async Task Desactivar(Usuario usuario, CancellationToken cancellationToken)
    {
        if (usuario.CambiarEstado(false, usuarioActual.Username, reloj.Ahora).IsOk)
            await repositorio.Actualizar(usuario, cancellationToken);
    }

    private async Task<bool> EnviarCredenciales(
        Usuario usuario,
        string passwordTemporal,
        CancellationToken cancellationToken)
    {
        try
        {
            var envio = await enviadorCorreo.EnviarCredenciales(
                usuario.Correo, usuario.Username, passwordTemporal, cancellationToken);

            return envio.IsOk;
        }
        catch (Exception excepcion) when (
            excepcion is InvalidOperationException or ArgumentException)
        {
            return false;
        }
    }
}
