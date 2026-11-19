using MongoDB.Driver;

namespace Proveedores.Infrastructure.Mongo;

/// <summary>Índices de la colección. Se crean de forma idempotente al arrancar (MongoStartupInitializer).</summary>
public static class ProveedorIndexes
{
    public const string NombreActivoUnico = "ux_proveedores_nombre_activo";
    public const string FechaCreacion = "ix_proveedores_fechaCreacion";
    public const string OrigenSeed = "ix_proveedores_origenSeed";

    internal static async Task CrearAsync(IMongoCollection<ProveedorDocument> coleccion, CancellationToken ct)
    {
        var keys = Builders<ProveedorDocument>.IndexKeys;
        var filtro = Builders<ProveedorDocument>.Filter;

        // Equivalente al UNIQUE sobre la columna virtual nombreActivo del SQL:
        // único SOLO entre proveedores activos (índice parcial). Protege también ante concurrencia.
        var unicoActivos = new CreateIndexModel<ProveedorDocument>(
            keys.Ascending(x => x.NombreNormalizado),
            new CreateIndexOptions<ProveedorDocument>
            {
                Name = NombreActivoUnico,
                Unique = true,
                PartialFilterExpression = filtro.Eq(x => x.EstaActivo, true)
            });

        var porFecha = new CreateIndexModel<ProveedorDocument>(
            keys.Descending(x => x.FechaCreacion), new CreateIndexOptions { Name = FechaCreacion });

        var porSeed = new CreateIndexModel<ProveedorDocument>(
            keys.Ascending(x => x.OrigenSeed),
            new CreateIndexOptions<ProveedorDocument>
            {
                Name = OrigenSeed,
                Sparse = true
            });

        await coleccion.Indexes.CreateManyAsync(new[] { unicoActivos, porFecha, porSeed }, ct);
    }
}
