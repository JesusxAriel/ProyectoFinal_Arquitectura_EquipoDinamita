using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Services;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.CambiarPassword;

public sealed class CambiarPasswordCasoDeUso(
    IUsuarioRepositorio repositorio,
    IGestorDeIdentidad gestorDeIdentidad,
    IUsuarioActual usuarioActual,
    IReloj reloj)
{
    public const string UsuarioNoRegistrado = "El usuario autenticado no está registrado en el sistema.";
    public const string UsuarioInactivo = "El usuario está inactivo y no puede cambiar su contraseña.";
    public const string PasswordRepetida = "La contraseña nueva debe ser distinta de la actual.";

    private const string CampoPasswordActual = "La contraseña actual";

    public async Task<Result> EjecutarAsync(
        CambiarPasswordEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var actual = entrada.Actual ?? "";
        var nueva = entrada.Nueva ?? "";
        var confirmacion = entrada.Confirmacion ?? "";

        var validacion = Validaciones.PrimerError(
            Validaciones.NoVacio(actual, CampoPasswordActual),
            PoliticaDePassword.Coinciden(nueva, confirmacion),
            PoliticaDePassword.Validar(nueva),
            Distinta(actual, nueva));

        if (validacion.IsFail)
            return validacion;

        var usuario = await repositorio.ObtenerPorUsername(usuarioActual.Username, cancellationToken);
        if (usuario is null)
            return Result.Fail(UsuarioNoRegistrado);

        if (!usuario.Activo)
            return Result.Fail(UsuarioInactivo);

        var verificacion = await gestorDeIdentidad.VerificarPassword(usuario.Username, actual);
        if (verificacion.IsFail)
            return verificacion;

        var cambio = await gestorDeIdentidad.CambiarPassword(usuario.Username, nueva);
        if (cambio.IsFail)
            return cambio;

        var marca = usuario.PasswordCambiada(usuario.Username, reloj.Ahora);
        if (marca.IsFail)
            return marca;

        var guardado = await repositorio.Actualizar(usuario, cancellationToken);

        return guardado.IsFail ? Result.Fail(guardado.Error) : Result.Ok();
    }

    private static Result Distinta(string actual, string nueva) =>
        string.Equals(actual, nueva, StringComparison.Ordinal)
            ? Result.Fail(PasswordRepetida)
            : Result.Ok();
}
