using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ConsultarCambioObligatorio;

public sealed class ConsultarCambioObligatorioCasoDeUso(
    IUsuarioRepositorio repositorio,
    IUsuarioActual usuarioActual)
{
    public async Task<Result<bool>> EjecutarAsync(
        ConsultarCambioObligatorioEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var usuario = await repositorio.ObtenerPorUsername(usuarioActual.Username, cancellationToken);

        return Result<bool>.Ok(usuario is { DebeCambiarPassword: true });
    }
}
