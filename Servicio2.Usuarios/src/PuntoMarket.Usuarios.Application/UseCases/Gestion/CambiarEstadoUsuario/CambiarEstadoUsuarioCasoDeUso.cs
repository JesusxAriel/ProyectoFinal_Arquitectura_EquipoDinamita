using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.CambiarEstadoUsuario;

public sealed class CambiarEstadoUsuarioCasoDeUso(
    IUsuarioRepositorio repositorio,
    IGestorDeIdentidad gestorDeIdentidad,
    IUsuarioActual usuarioActual,
    IReloj reloj)
{
    public const string NoPuedeDesactivarseASiMismo = "No puede desactivar su propia cuenta.";

    public async Task<Result<CambiarEstadoUsuarioSalida>> EjecutarAsync(
        CambiarEstadoUsuarioEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var usuario = await repositorio.ObtenerPorId(entrada.Id, cancellationToken);
        if (usuario is null)
            return Result<CambiarEstadoUsuarioSalida>.Fail(MensajesDeGestion.UsuarioNoEncontrado);

        if (!entrada.Activo && EsElUsuarioActual(usuario.Username))
            return Result<CambiarEstadoUsuarioSalida>.Fail(NoPuedeDesactivarseASiMismo);

        var cambio = usuario.CambiarEstado(entrada.Activo, usuarioActual.Username, reloj.Ahora);
        if (cambio.IsFail)
            return Result<CambiarEstadoUsuarioSalida>.Fail(cambio.Error);

        var identidad = await gestorDeIdentidad.CambiarEstado(usuario.Username, entrada.Activo);
        if (identidad.IsFail)
            return Result<CambiarEstadoUsuarioSalida>.Fail(identidad.Error);

        var guardado = await repositorio.Actualizar(usuario, cancellationToken);
        if (guardado.IsFail)
            return Result<CambiarEstadoUsuarioSalida>.Fail(guardado.Error);

        return Result<CambiarEstadoUsuarioSalida>.Ok(new CambiarEstadoUsuarioSalida(usuario.Activo));
    }

    private bool EsElUsuarioActual(string username) =>
        string.Equals(username, usuarioActual.Username, StringComparison.OrdinalIgnoreCase);
}
