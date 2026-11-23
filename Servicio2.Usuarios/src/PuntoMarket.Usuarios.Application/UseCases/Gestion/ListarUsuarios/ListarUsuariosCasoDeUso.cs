using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;

public sealed class ListarUsuariosCasoDeUso(IUsuarioRepositorio repositorio)
{
    public async Task<Result<ListarUsuariosSalida>> EjecutarAsync(
        ListarUsuariosEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var usuarios = await repositorio.Listar(cancellationToken);
        var roles = await repositorio.ListarRoles(cancellationToken);
        var nombrePorRol = roles.ToDictionary(rol => rol.Id, rol => rol.Nombre);

        var salida = usuarios
            .Select(usuario => UsuarioSalida.Desde(usuario, NombreDelRol(nombrePorRol, usuario.RolId)))
            .ToArray();

        return Result<ListarUsuariosSalida>.Ok(new ListarUsuariosSalida(salida));
    }

    private static string NombreDelRol(IReadOnlyDictionary<int, string> nombrePorRol, int rolId) =>
        nombrePorRol.TryGetValue(rolId, out var nombre) ? nombre : MensajesDeGestion.SinRolAsignado;
}
