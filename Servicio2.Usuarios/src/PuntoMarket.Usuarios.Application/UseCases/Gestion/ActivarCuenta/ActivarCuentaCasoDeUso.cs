using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Services;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ActivarCuenta;

public sealed class ActivarCuentaCasoDeUso(
    IActivacionRepositorio activaciones,
    IUsuarioRepositorio repositorio,
    IGestorDeIdentidad gestorDeIdentidad,
    IGeneradorTokenActivacion generadorToken,
    IReloj reloj)
{
    public const string EnlaceInvalido =
        "El enlace de activación no es válido o ha vencido. Solicite uno nuevo al administrador.";

    public const string ActivacionNoCompletada =
        "No se pudo completar la activación. Intente nuevamente más tarde.";

    public async Task<Result<ActivarCuentaSalida>> EjecutarAsync(
        ActivarCuentaEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var validacion = Validaciones.PrimerError(
            PoliticaDePassword.Coinciden(entrada.NuevaContrasena, entrada.ConfirmarContrasena),
            PoliticaDePassword.Validar(entrada.NuevaContrasena));

        if (validacion.IsFail)
            return Result<ActivarCuentaSalida>.Fail(validacion.Error);

        if (string.IsNullOrWhiteSpace(entrada.Token))
            return Result<ActivarCuentaSalida>.Fail(EnlaceInvalido);

        var hash = generadorToken.CalcularHash(entrada.Token.Trim());
        var consumido = await activaciones.ConsumirToken(hash, reloj.Ahora, cancellationToken);
        if (consumido is null)
            return Result<ActivarCuentaSalida>.Fail(EnlaceInvalido);

        var usuario = consumido.Usuario;

        var identidad = await gestorDeIdentidad.CambiarPassword(usuario.Username, entrada.NuevaContrasena!);
        if (identidad.IsFail)
            return await Revertir(usuario.Id, hash, consumido.ExpiraUtc, cancellationToken);

        var marca = usuario.PasswordCambiada(usuario.Username, reloj.Ahora);
        if (marca.IsFail)
            return await Revertir(usuario.Id, hash, consumido.ExpiraUtc, cancellationToken);

        var guardado = await repositorio.Actualizar(usuario, cancellationToken);
        if (guardado.IsFail)
            return await Revertir(usuario.Id, hash, consumido.ExpiraUtc, cancellationToken);

        return Result<ActivarCuentaSalida>.Ok(new ActivarCuentaSalida(true));
    }

    private async Task<Result<ActivarCuentaSalida>> Revertir(
        int usuarioId,
        string hash,
        DateTime expiraUtc,
        CancellationToken cancellationToken)
    {
        await activaciones.RestaurarToken(usuarioId, hash, expiraUtc, cancellationToken);
        return Result<ActivarCuentaSalida>.Fail(ActivacionNoCompletada);
    }
}
