using Npgsql;
namespace PuntoMarket.Productos.Adapters.Out.Persistence;

public sealed class ProductosDatabaseInitializer(NpgsqlDataSource source)
{
    public async Task InicializarAsync(bool seedDesarrollo, CancellationToken ct = default)
    {
        await using var conn = await source.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        // Serializa la migración entre réplicas del servicio.
        await using(var cmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(703001)",conn,tx)) await cmd.ExecuteNonQueryAsync(ct);
        var assembly=typeof(ProductosDatabaseInitializer).Assembly;
        foreach(var nombre in new[]{"001_productos.sql",seedDesarrollo?"Seed_desarrollo.sql":null}.Where(x=>x is not null)) {
            var recurso=assembly.GetManifestResourceNames().Single(n=>n.EndsWith(nombre!,StringComparison.Ordinal));
            using var stream=assembly.GetManifestResourceStream(recurso)!;
            using var reader=new StreamReader(stream);
            await using var cmd=new NpgsqlCommand(await reader.ReadToEndAsync(ct),conn,tx);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
}
