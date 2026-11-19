using Proveedores.Application.Dtos;
using Proveedores.Domain.Common;
using Proveedores.Domain.Entities;

namespace Proveedores.Application.Abstractions;

/// <summary>Página de proveedores devuelta por el puerto de persistencia.</summary>
public sealed record PaginaProveedores(IReadOnlyList<Proveedor> Items, long Total);

/// <summary>
/// PUERTO de persistencia (lo define Application; lo implementa Infrastructure).
/// Adaptador real: Proveedores.Infrastructure/Mongo/MongoProveedorRepository.cs (patrón ADAPTER).
/// Las violaciones ESPERADAS de unicidad se devuelven como Result.Failure (Conflict); los fallos técnicos lanzan excepción.
/// </summary>
public interface IProveedorRepository
{
    Task<Proveedor?> ObtenerPorIdAsync(string id, CancellationToken cancellationToken);

    Task<PaginaProveedores> ListarAsync(ListarProveedoresQuery consulta, CancellationToken cancellationToken);

    Task<bool> ExisteActivoConNombreAsync(string nombreNormalizado, string? excluirId, CancellationToken cancellationToken);

    /// <summary>Inserta y asigna el identificador. Devuelve Conflict si el índice único detecta un nombre activo duplicado.</summary>
    Task<Result<Proveedor>> AgregarAsync(Proveedor proveedor, CancellationToken cancellationToken);

    /// <summary>Guarda solo si coincide Version y la incrementa al aceptar. Devuelve Conflict ante cambios concurrentes o nombre duplicado.</summary>
    Task<Result> ActualizarAsync(Proveedor proveedor, CancellationToken cancellationToken);
}
