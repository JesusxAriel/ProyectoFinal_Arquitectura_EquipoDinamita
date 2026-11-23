using Npgsql;
using PuntoMarket.Productos.Adapters.Out.Persistence;
using PuntoMarket.Productos.Domain;
namespace PuntoMarket.Productos.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute() { if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PRODUCTOS_TEST_POSTGRES"))) Skip="Configure PRODUCTOS_TEST_POSTGRES con una base PostgreSQL exclusiva de pruebas."; }
}
public class PostgresPersistenceTests
{
    [PostgresFact]
    public async Task CrudMigracionHistorialYRollbackEnEsquemaAislado()
    {
        var connection=Environment.GetEnvironmentVariable("PRODUCTOS_TEST_POSTGRES")!;
        var schema="productos_test_"+Guid.NewGuid().ToString("N");
        await using var admin=NpgsqlDataSource.Create(connection);
        await using(var create=admin.CreateCommand($"CREATE SCHEMA {schema}")) await create.ExecuteNonQueryAsync();
        try {
            var config=new NpgsqlConnectionStringBuilder(connection){SearchPath=schema};
            await using var ds=NpgsqlDataSource.Create(config.ConnectionString);
            var init=new ProductosDatabaseInitializer(ds);
            await init.InicializarAsync(true); await init.InicializarAsync(true);
            var repo=new PostgresProductoRepository(ds);
            var catalogs=await repo.CatalogosAsync(default);
            Assert.Equal(3,catalogs.Value.Categorias.Count); Assert.Equal(3,catalogs.Value.Empaques.Count);
            var data=ReglasYCasesTests.Datos with{CategoriaId=catalogs.Value.Categorias[0].Id,EmpaqueId=catalogs.Value.Empaques[0].Id};
            var created=await repo.CrearAsync(data,"sub-creador",default);
            Assert.True(created.IsSuccess,created.Error.Message); var id=created.Value.Id;
            Assert.Equal("sub-creador",created.Value.IdUsuarioCreacion);
            Assert.Equal("Producto.CodigoDuplicado",(await repo.CrearAsync(data,"otro",default)).Error.Code);
            Assert.Equal(ErrorType.Validation,(await repo.CrearAsync(data with{Codigo="OTRO",CategoriaId=int.MaxValue},"otro",default)).Error.Type);
            Assert.True((await repo.ActualizarAsync(id,data with{PrecioVenta=16},"sub-editor",default)).IsFailure);
            var edited=await repo.ActualizarAsync(id,data with{PrecioVenta=16,MotivoCambioPrecio="Ajuste de prueba"},"sub-editor",default);
            Assert.True(edited.IsSuccess,edited.Error.Message); Assert.Equal("sub-editor",edited.Value.IdUsuarioActualizacion);
            await using(var count=ds.CreateCommand("SELECT count(*) FROM historial_precio WHERE motivo='Ajuste de prueba' AND usuario_id='sub-editor' AND precio_anterior=15 AND precio_nuevo=16"))
                Assert.Equal(1L,await count.ExecuteScalarAsync());
            // Fuerza fallo técnico en el historial para demostrar rollback de la edición.
            await using(var trigger=ds.CreateCommand("""
                CREATE FUNCTION rechazar_historial() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'fallo técnico de prueba'; END $$;
                CREATE TRIGGER rechazar_historial BEFORE INSERT ON historial_precio FOR EACH ROW EXECUTE FUNCTION rechazar_historial();
                """)) await trigger.ExecuteNonQueryAsync();
            Assert.True((await repo.ActualizarAsync(id,data with{PrecioVenta=17,MotivoCambioPrecio="Rollback"},"sub-editor",default)).IsFailure);
            Assert.Equal(16,(await repo.ObtenerAsync(id,default)).Value.PrecioVenta);
            await init.InicializarAsync(true);
            Assert.Equal(id,(await repo.ListarAsync(new(Busqueda:"SKU",OrdenarPor:"PrecioVenta",Descendente:true),default)).Value.Items.Single().Id);
            Assert.True((await repo.DesactivarAsync(id,"sub-baja",default)).IsSuccess);
            var inactive=(await repo.ObtenerAsync(id,default)).Value;
            Assert.False(inactive.EstaActivo); Assert.Equal("sub-baja",inactive.IdUsuarioActualizacion);
            Assert.Equal("Producto.Inactivo",(await repo.DesactivarAsync(id,"sub-baja",default)).Error.Code);
            Assert.Empty((await repo.ListarAsync(new(),default)).Value.Items);
            Assert.Single((await repo.ListarAsync(new(Estado:"Inactivos"),default)).Value.Items);
        } finally {
            // El nombre solo procede del GUID generado aquí; nunca se elimina otro esquema.
            await using var drop=admin.CreateCommand($"DROP SCHEMA {schema} CASCADE");
            await drop.ExecuteNonQueryAsync();
        }
    }
}
