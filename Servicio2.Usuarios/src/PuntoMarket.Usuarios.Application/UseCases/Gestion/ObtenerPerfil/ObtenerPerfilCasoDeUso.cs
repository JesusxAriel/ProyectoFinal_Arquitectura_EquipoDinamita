using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ObtenerPerfil;

public sealed class ObtenerPerfilCasoDeUso(
    IUsuarioRepositorio repositorio,
    IUsuarioActual usuarioActual)
{
    public const string UsuarioNoRegistrado = "El usuario autenticado no está registrado en el sistema.";
    public const string RolNoDisponible = "El rol del usuario no está disponible.";

    public async Task<Result<ObtenerPerfilSalida>> EjecutarAsync(
        ObtenerPerfilEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var usuario = await repositorio.ObtenerPorUsername(usuarioActual.Username, cancellationToken);
        if (usuario is null)
            return Result<ObtenerPerfilSalida>.Fail(UsuarioNoRegistrado);

        var rol = await repositorio.ObtenerRol(usuario.RolId, cancellationToken);
        if (rol is null)
            return Result<ObtenerPerfilSalida>.Fail(RolNoDisponible);

        return Result<ObtenerPerfilSalida>.Ok(new ObtenerPerfilSalida(
            usuario.Username,
            usuario.NombreCompleto,
            rol.Nombre,
            usuario.Activo,
            usuario.DebeCambiarPassword));
    }
}
