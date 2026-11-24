using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.VerificarPasswordActual;

public sealed class VerificarPasswordActualCasoDeUso(
    IGestorDeIdentidad gestorDeIdentidad,
    IUsuarioActual usuarioActual)
{
    public async Task<Result<VerificarPasswordActualSalida>> EjecutarAsync(
        VerificarPasswordActualEntrada entrada)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        if (string.IsNullOrEmpty(entrada.Actual))
            return Result<VerificarPasswordActualSalida>.Ok(new VerificarPasswordActualSalida(false));

        var verificacion = await gestorDeIdentidad.VerificarPassword(usuarioActual.Username, entrada.Actual);
        if (verificacion.IsOk)
            return Result<VerificarPasswordActualSalida>.Ok(new VerificarPasswordActualSalida(true));

        return verificacion.Error == MensajesDeGestion.PasswordActualIncorrecta
            ? Result<VerificarPasswordActualSalida>.Ok(new VerificarPasswordActualSalida(false))
            : Result<VerificarPasswordActualSalida>.Fail(verificacion.Error);
    }
}
