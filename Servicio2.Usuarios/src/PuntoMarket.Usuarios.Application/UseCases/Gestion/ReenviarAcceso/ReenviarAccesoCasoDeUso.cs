using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ReenviarAcceso;

public sealed class ReenviarAccesoCasoDeUso(
    IUsuarioRepositorio repositorio,
    IGestorDeIdentidad gestorDeIdentidad,
    IEnviadorCorreo enviadorCorreo,
    IGeneradorPassword generadorPassword,
    IUsuarioActual usuarioActual,
    IReloj reloj)
{
    public const string UsuarioInactivo =
        "El usuario está inactivo. Actívelo antes de reenviar su acceso.";

    public async Task<Result<ReenviarAccesoSalida>> EjecutarAsync(
        ReenviarAccesoEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var usuario = await repositorio.ObtenerPorId(entrada.Id, cancellationToken);
        if (usuario is null)
            return Result<ReenviarAccesoSalida>.Fail(MensajesDeGestion.UsuarioNoEncontrado);

        if (!usuario.Activo)
            return Result<ReenviarAccesoSalida>.Fail(UsuarioInactivo);

        var passwordTemporal = generadorPassword.Generar();

        var identidad = await gestorDeIdentidad.CambiarPassword(usuario.Username, passwordTemporal);
        if (identidad.IsFail)
            return Result<ReenviarAccesoSalida>.Fail(identidad.Error);

        var marca = usuario.AccesoRestablecido(usuarioActual.Username, reloj.Ahora);
        if (marca.IsFail)
            return Result<ReenviarAccesoSalida>.Fail(marca.Error);

        var guardado = await repositorio.Actualizar(usuario, cancellationToken);
        if (guardado.IsFail)
            return Result<ReenviarAccesoSalida>.Fail(guardado.Error);

        var correoEnviado = await EnviarCredenciales(usuario.Correo, usuario.Username, passwordTemporal, cancellationToken);

        return Result<ReenviarAccesoSalida>.Ok(new ReenviarAccesoSalida(correoEnviado));
    }

    private async Task<bool> EnviarCredenciales(
        string correo,
        string username,
        string passwordTemporal,
        CancellationToken cancellationToken)
    {
        try
        {
            var envio = await enviadorCorreo.EnviarCredenciales(correo, username, passwordTemporal, cancellationToken);

            return envio.IsOk;
        }
        catch (Exception excepcion) when (
            excepcion is InvalidOperationException or ArgumentException)
        {
            return false;
        }
    }
}
