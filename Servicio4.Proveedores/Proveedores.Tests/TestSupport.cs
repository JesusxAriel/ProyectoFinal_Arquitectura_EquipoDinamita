using Proveedores.Application.Abstractions;
using Proveedores.Application.Dtos;
using Proveedores.Domain.Common;
using Proveedores.Domain.Entities;
using Proveedores.Domain.Errors;

namespace Proveedores.Tests;

/// <summary>Reloj controlable para pruebas deterministas.</summary>
public sealed class FixedTimeProvider(DateTimeOffset ahora) : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = ahora;
    public override DateTimeOffset GetUtcNow() => Ahora;
}

public sealed class FakeCurrentUser(string? userId) : ICurrentUser
{
    public string? UserId { get; set; } = userId;
}

/// <summary>
/// Repositorio en memoria que respeta el contrato del puerto, incluido el comportamiento del índice
/// único parcial (nombre normalizado único entre proveedores activos). No requiere Docker ni MongoDB.
/// </summary>
public sealed class InMemoryProveedorRepository : IProveedorRepository
{
    private readonly List<Proveedor> _datos = [];
    private readonly object _candado = new();
    private int _secuencia;

    private static Proveedor Copia(Proveedor p) => Proveedor.Rehidratar(
        p.Id, p.NombreEmpresa, p.NumeroEmpresa, p.CorreoReferencia, p.EsAutogestionado,
        p.EstaActivo, p.IdUsuarioAdmin, p.IdUsuarioActualizacion, p.FechaCreacion, p.FechaActualizacion, p.Version);

    public IReadOnlyList<Proveedor> Todos { get { lock (_candado) return _datos.Select(Copia).ToList(); } }

    public Task<Proveedor?> ObtenerPorIdAsync(string id, CancellationToken cancellationToken)
    {
        lock (_candado) return Task.FromResult(_datos.FirstOrDefault(p => p.Id == id) is { } p ? Copia(p) : null);
    }

    public Task<PaginaProveedores> ListarAsync(ListarProveedoresQuery c, CancellationToken cancellationToken)
    {
        lock (_candado)
        {
            IEnumerable<Proveedor> q = _datos;
            if (!string.IsNullOrWhiteSpace(c.Busqueda))
            {
                var t = NombreNormalizador.Normalizar(c.Busqueda);
                q = q.Where(p => p.NombreNormalizado.Contains(t) || p.NumeroEmpresa.Contains(c.Busqueda, StringComparison.OrdinalIgnoreCase)
                    || p.CorreoReferencia?.Contains(c.Busqueda, StringComparison.OrdinalIgnoreCase) == true);
            }
            q = c.Estado switch
            {
                EstadoFiltro.Activos => q.Where(p => p.EstaActivo),
                EstadoFiltro.Inactivos => q.Where(p => !p.EstaActivo),
                _ => q
            };
            q = (c.OrdenarPor, c.Descendente) switch
            {
                (CampoOrden.Numero, false) => q.OrderBy(p => p.NumeroEmpresa),
                (CampoOrden.Numero, true) => q.OrderByDescending(p => p.NumeroEmpresa),
                (CampoOrden.FechaCreacion, false) => q.OrderBy(p => p.FechaCreacion),
                (CampoOrden.FechaCreacion, true) => q.OrderByDescending(p => p.FechaCreacion),
                (_, true) => q.OrderByDescending(p => p.NombreNormalizado),
                _ => q.OrderBy(p => p.NombreNormalizado)
            };
            var lista = q.ToList();
            var normalizada = c.Normalizada();
            var items = lista.Skip(normalizada.Desplazamiento).Take(normalizada.TamanoPagina).Select(Copia).ToList();
            return Task.FromResult(new PaginaProveedores(items, lista.Count));
        }
    }

    public Task<bool> ExisteActivoConNombreAsync(string nombreNormalizado, string? excluirId, CancellationToken cancellationToken)
    {
        lock (_candado)
            return Task.FromResult(_datos.Any(p => p.EstaActivo && p.NombreNormalizado == nombreNormalizado && p.Id != excluirId));
    }

    public Task<Result<Proveedor>> AgregarAsync(Proveedor proveedor, CancellationToken cancellationToken)
    {
        lock (_candado)
        {
            if (_datos.Any(p => p.EstaActivo && p.NombreNormalizado == proveedor.NombreNormalizado))
                return Task.FromResult(Result.Failure<Proveedor>(ProveedorErrors.NombreDuplicado));

            proveedor.AsignarIdentificador((++_secuencia).ToString("x24"));
            _datos.Add(Copia(proveedor));
            return Task.FromResult(Result.Success(proveedor));
        }
    }

    public Task<Result> ActualizarAsync(Proveedor proveedor, CancellationToken cancellationToken)
    {
        lock (_candado)
        {
            if (_datos.All(p => p.Id != proveedor.Id))
                return Task.FromResult(Result.Failure(ProveedorErrors.NoEncontrado));
            if (proveedor.EstaActivo && _datos.Any(p => p.Id != proveedor.Id && p.EstaActivo && p.NombreNormalizado == proveedor.NombreNormalizado))
                return Task.FromResult(Result.Failure(ProveedorErrors.NombreDuplicado));
            var indice = _datos.FindIndex(p => p.Id == proveedor.Id);
            if (_datos[indice].Version != proveedor.Version)
                return Task.FromResult(Result.Failure(ProveedorErrors.CambioConcurrente));
            proveedor.ConfirmarEscritura();
            _datos[indice] = Copia(proveedor);
            return Task.FromResult(Result.Success());
        }
    }
}

public static class Datos
{
    public static DatosProveedor Valido(string nombre = "Distribuidora Andina", string numero = "70012345",
        string? correo = "contacto@andina.example.invalid", bool autogestionado = false) =>
        new(nombre, numero, correo, autogestionado);

    public static GuardarProveedorRequest Request(string nombre = "Distribuidora Andina", string numero = "70012345",
        string? correo = "contacto@andina.example.invalid", bool autogestionado = false) =>
        new(nombre, numero, correo, autogestionado);
}
