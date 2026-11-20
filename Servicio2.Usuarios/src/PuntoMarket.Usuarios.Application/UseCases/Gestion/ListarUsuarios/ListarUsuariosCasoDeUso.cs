using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain.Enums;
using PuntoMarket.Usuarios.Domain.Services;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;

public sealed class ListarUsuariosCasoDeUso(
    IUsuarioGestionRepositorio repositorio,
    IReloj reloj)
{
    public async Task<ListarUsuariosSalida> EjecutarAsync(
        ListarUsuariosEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        cancellationToken.ThrowIfCancellationRequested();

        var usuarios = await repositorio.ListarAsync(cancellationToken);
        var ahoraLocal = ZonaHorariaDeLaPaz.ALocal(reloj.Ahora);
        var salida = usuarios.Select(usuario =>
        {
            var estaExpirado = usuario.TokenActivacionExpira is DateTime vence &&
                string.Equals(usuario.Estado, nameof(EstadoUsuario.PENDIENTE), StringComparison.OrdinalIgnoreCase) &&
                vence <= ahoraLocal;

            return new UsuarioListadoSalida(
                usuario.Id,
                usuario.Nombre,
                usuario.Apellido,
                usuario.Correo,
                usuario.Username,
                usuario.Rol,
                estaExpirado ? "Expirado" : usuario.Estado,
                usuario.FechaCreacion,
                usuario.TokenActivacionExpira,
                usuario.FechaActivacion,
                estaExpirado);
        }).ToArray();

        return new ListarUsuariosSalida(salida);
    }
}
