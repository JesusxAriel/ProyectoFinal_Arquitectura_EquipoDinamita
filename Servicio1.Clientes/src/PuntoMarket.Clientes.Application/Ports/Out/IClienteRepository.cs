using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Entities;

namespace PuntoMarket.Clientes.Application.Ports.Out;

/// <summary>Página de clientes devuelta por el puerto de persistencia.</summary>
public sealed record PaginaClientes(IReadOnlyList<Cliente> Items, long Total);

/// <summary>
/// PUERTO DE SALIDA de persistencia (lo define Application; lo implementa el adaptador).
/// Adaptador real: Adapters.Out.Persistence/Repositories/MySqlClienteRepository.cs (patrón ADAPTER).
/// Las violaciones ESPERADAS de unicidad se devuelven como Result.Failure (Conflict); los fallos técnicos lanzan excepción.
/// </summary>
public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    Task<PaginaClientes> ListarAsync(ListarClientesQuery consulta, CancellationToken cancellationToken);

    Task<bool> ExisteActivoConNitCiAsync(string nitCi, int? excluirId, CancellationToken cancellationToken);

    /// <summary>Inserta y asigna el identificador. Devuelve Conflict si el índice único detecta un NIT/CI activo duplicado.</summary>
    Task<Result<Cliente>> AgregarAsync(Cliente cliente, CancellationToken cancellationToken);

    /// <summary>Guarda los cambios. Devuelve NotFound si ya no existe y Conflict si el NIT/CI activo está duplicado.</summary>
    Task<Result> ActualizarAsync(Cliente cliente, CancellationToken cancellationToken);
}
