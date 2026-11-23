using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ObtenerUsuario;

public sealed class ObtenerUsuarioCasoDeUso(IUsuarioRepositorio repositorio)
{
    public async Task<Result<ObtenerUsuarioSalida>> EjecutarAsync(
        ObtenerUsuarioEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var usuario = await repositorio.ObtenerPorId(entrada.Id, cancellationToken);
        if (usuario is null)
            return Result<ObtenerUsuarioSalida>.Fail(MensajesDeGestion.UsuarioNoEncontrado);

        var rol = await repositorio.ObtenerRol(usuario.RolId, cancellationToken);

        return Result<ObtenerUsuarioSalida>.Ok(new ObtenerUsuarioSalida(
            UsuarioSalida.Desde(usuario, rol?.Nombre ?? MensajesDeGestion.SinRolAsignado)));
    }
}
