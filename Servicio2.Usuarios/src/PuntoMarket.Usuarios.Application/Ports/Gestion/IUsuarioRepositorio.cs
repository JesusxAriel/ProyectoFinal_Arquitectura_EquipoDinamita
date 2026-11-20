using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;

namespace PuntoMarket.Usuarios.Application.Ports.Gestion;

public interface IUsuarioRepositorio
{
    Task<Result<Usuario>> Guardar(Usuario usuario, CancellationToken cancellationToken = default);

    Task<Result<Usuario>> Actualizar(Usuario usuario, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorId(int id, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorUsername(string username, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Usuario>> Listar(CancellationToken cancellationToken = default);

    Task<bool> ExisteUsername(string username, CancellationToken cancellationToken = default);

    Task<bool> ExisteCorreo(string correo, CancellationToken cancellationToken = default);

    Task<Rol?> ObtenerRol(int rolId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Rol>> ListarRolesActivos(CancellationToken cancellationToken = default);
}
