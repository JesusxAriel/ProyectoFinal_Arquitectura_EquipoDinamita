using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ReenviarAcceso;

public sealed class ReenviarAccesoCasoDeUso(
    IUsuarioRepositorio repositorio,
    IGestorDeIdentidad gestorDeIdentidad,
    IEnviadorCorreo enviadorCorreo,
    IGeneradorPassword generadorPassword,
    IGeneradorTokenActivacion generadorToken,
    IActivacionRepositorio activaciones,
    ConfiguracionActivacion configuracionActivacion,
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

        var passwordDesconocida = generadorPassword.Generar();

        var identidad = await gestorDeIdentidad.CambiarPassword(usuario.Username, passwordDesconocida);
        if (identidad.IsFail)
            return Result<ReenviarAccesoSalida>.Fail(identidad.Error);

        var marca = usuario.AccesoRestablecido(usuarioActual.Username, reloj.Ahora);
        if (marca.IsFail)
            return Result<ReenviarAccesoSalida>.Fail(marca.Error);

        var guardado = await repositorio.Actualizar(usuario, cancellationToken);
        if (guardado.IsFail)
            return Result<ReenviarAccesoSalida>.Fail(guardado.Error);

        var token = generadorToken.Generar(reloj.Ahora, configuracionActivacion.Vigencia);
        var tokenGuardado = await activaciones.GuardarToken(usuario.Id, token.Hash, token.ExpiraUtc, cancellationToken);
        if (tokenGuardado.IsFail)
            return Result<ReenviarAccesoSalida>.Fail(tokenGuardado.Error);

        var correoEnviado = await EnviarActivacion(usuario, token.Valor, cancellationToken);

        return Result<ReenviarAccesoSalida>.Ok(new ReenviarAccesoSalida(correoEnviado));
    }

    private async Task<bool> EnviarActivacion(Usuario usuario, string token, CancellationToken cancellationToken)
    {
        try
        {
            var envio = await enviadorCorreo.EnviarActivacion(
                usuario.Correo, usuario.Username, token, configuracionActivacion.Vigencia, cancellationToken);

            return envio.IsOk;
        }
        catch (Exception excepcion) when (
            excepcion is InvalidOperationException or ArgumentException)
        {
            return false;
        }
    }
}
