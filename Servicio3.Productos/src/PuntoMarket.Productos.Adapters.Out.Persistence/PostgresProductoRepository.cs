using Npgsql;
using PuntoMarket.Productos.Application;
using PuntoMarket.Productos.Application.Ports.Out;
using PuntoMarket.Productos.Domain;
namespace PuntoMarket.Productos.Adapters.Out.Persistence;

public sealed class PostgresProductoRepositoryCreator(NpgsqlDataSource source) : ProductoRepositoryCreator
{
    public override IProductoRepository Crear() => new PostgresProductoRepository(source);
}
public sealed class PostgresProductoRepository(NpgsqlDataSource source) : IProductoRepository
{
    private const string Columnas = "id,codigo,nombre,costo,precio_venta,categoria_id,empaque_id,proveedor_id,esta_activo,id_usuario_creacion,id_usuario_actualizacion,fecha_creacion,fecha_actualizacion";
    private static Producto Leer(NpgsqlDataReader r) => new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetDecimal(3),r.GetDecimal(4),r.GetInt32(5),r.GetInt32(6),r.GetString(7),r.GetBoolean(8),r.GetString(9),r.IsDBNull(10)?null:r.GetString(10),r.GetDateTime(11),r.IsDBNull(12)?null:r.GetDateTime(12));
    private static Error NoExiste => Error.NotFound("Producto.NoEncontrado", "El producto no existe.");
    private static Error Inactivo => Error.Conflict("Producto.Inactivo", "El producto ya está desactivado.");
    private static Error Traducir(Exception e) => e switch {
        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } => Error.Conflict("Producto.CodigoDuplicado", "Ya existe un producto con ese código."),
        PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } => Error.Validation("Producto.CatalogoInvalido", "La categoría o el empaque no existe."),
        PostgresException { SqlState: PostgresErrorCodes.CheckViolation } => Error.Validation("Producto.DatosInvalidos", "Los datos no cumplen las reglas del producto."),
        _ => Error.Unavailable("Producto.PersistenciaNoDisponible", "La persistencia de Productos no está disponible. Intente nuevamente.")
    };
    private static bool Tecnico(Exception e) => e is NpgsqlException or TimeoutException;
    public async Task<Result<Producto>> ObtenerAsync(int id, CancellationToken ct)
    {
        try {
            await using var conn = await source.OpenConnectionAsync(ct);
            await using var cmd = new NpgsqlCommand($"SELECT {Columnas} FROM producto WHERE id=@id",conn);
            cmd.Parameters.AddWithValue("id", id);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Result.Success(Leer(reader)) : Result.Failure<Producto>(NoExiste);
        } catch(Exception e) when(Tecnico(e)) { return Result.Failure<Producto>(Traducir(e)); }
    }
    public async Task<Result<PaginaResultado<Producto>>> ListarAsync(ListarProductosQuery q, CancellationToken ct)
    {
        var normal = q.Normalizar();
        if(normal.IsFailure) return Result.Failure<PaginaResultado<Producto>>(normal.Error);
        q=normal.Value;
        // Lista permitida: nunca interpolar directamente la columna recibida.
        var columna = q.OrdenarPor switch { "Codigo"=>"codigo","Costo"=>"costo","PrecioVenta"=>"precio_venta","FechaCreacion"=>"fecha_creacion",_=>"nombre" };
        const string filtro = " WHERE (@estado='Todos' OR esta_activo=(@estado='Activos')) AND (@busqueda='' OR strpos(lower(nombre),lower(@busqueda))>0 OR strpos(lower(codigo),lower(@busqueda))>0)";
        try {
            await using var conn = await source.OpenConnectionAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,ct);
            await using var count = new NpgsqlCommand("SELECT count(*) FROM producto"+filtro, conn,tx);
            count.Parameters.AddWithValue("estado",q.Estado); count.Parameters.AddWithValue("busqueda",q.Busqueda??"");
            var total = (long)(await count.ExecuteScalarAsync(ct))!;
            await using var cmd = new NpgsqlCommand($"SELECT {Columnas} FROM producto{filtro} ORDER BY {columna} {(q.Descendente?"DESC":"ASC")},id ASC LIMIT @limite OFFSET @offset",conn,tx);
            cmd.Parameters.AddWithValue("estado",q.Estado); cmd.Parameters.AddWithValue("busqueda",q.Busqueda??"");
            cmd.Parameters.AddWithValue("limite",q.TamanoPagina); cmd.Parameters.AddWithValue("offset",(q.Pagina-1)*q.TamanoPagina);
            var items = new List<Producto>();
            await using(var reader=await cmd.ExecuteReaderAsync(ct)) { while(await reader.ReadAsync(ct)) items.Add(Leer(reader)); }
            await tx.CommitAsync(ct);
            return Result.Success(new PaginaResultado<Producto>(items,q.Pagina,q.TamanoPagina,total,(int)Math.Min(int.MaxValue,Math.Ceiling((decimal)total/q.TamanoPagina))));
        } catch(Exception e) when(Tecnico(e)) { return Result.Failure<PaginaResultado<Producto>>(Traducir(e)); }
    }
    public async Task<Result<CatalogosDto>> CatalogosAsync(CancellationToken ct)
    {
        try {
            await using var conn=await source.OpenConnectionAsync(ct);
            async Task<IReadOnlyList<CatalogoDto>> LeerCatalogo(string tabla) {
                await using var cmd = new NpgsqlCommand($"SELECT id,nombre FROM {tabla} ORDER BY nombre,id",conn);
                await using var r=await cmd.ExecuteReaderAsync(ct);
                var list=new List<CatalogoDto>(); while(await r.ReadAsync(ct)) list.Add(new(r.GetInt32(0),r.GetString(1))); return list;
            }
            return Result.Success(new CatalogosDto(await LeerCatalogo("categoria"), await LeerCatalogo("empaque")));
        } catch(Exception e) when(Tecnico(e)) { return Result.Failure<CatalogosDto>(Traducir(e)); }
    }
    public Task<Result<Producto>> CrearAsync(DatosProducto d,string usuario,CancellationToken ct) => GuardarAsync(null,d,usuario,ct);
    public Task<Result<Producto>> ActualizarAsync(int id,DatosProducto d,string usuario,CancellationToken ct) => GuardarAsync(id,d,usuario,ct);
    private async Task<Result<Producto>> GuardarAsync(int? id,DatosProducto d,string usuario,CancellationToken ct)
    {
        d=d.Normalizar();
        var validacion=ProductoReglas.Validar(d);
        if(validacion.IsFailure) return Result.Failure<Producto>(validacion.Error);
        if(string.IsNullOrWhiteSpace(usuario)) return Result.Failure<Producto>(Error.Unauthorized("Usuario.NoAutenticado","Se requiere una identidad autenticada."));
        try {
            await using var conn=await source.OpenConnectionAsync(ct);
            await using var tx=await conn.BeginTransactionAsync(ct);
            decimal? precioAnterior=null;
            if(id.HasValue) {
                await using var read=new NpgsqlCommand("SELECT precio_venta,esta_activo FROM producto WHERE id=@id FOR UPDATE",conn,tx);
                read.Parameters.AddWithValue("id",id.Value);
                await using var reader=await read.ExecuteReaderAsync(ct);
                if(!await reader.ReadAsync(ct)) return Result.Failure<Producto>(NoExiste);
                if(!reader.GetBoolean(1)) return Result.Failure<Producto>(Inactivo);
                precioAnterior=reader.GetDecimal(0);
            }
            // Revalidar con la fila bloqueada para proteger cambios concurrentes de precio.
            validacion=ProductoReglas.Validar(d,precioAnterior);
            if(validacion.IsFailure) return Result.Failure<Producto>(validacion.Error);
            var sql=id.HasValue
                ? $"UPDATE producto SET codigo=@codigo,nombre=@nombre,costo=@costo,precio_venta=@precio,categoria_id=@categoria,empaque_id=@empaque,proveedor_id=@proveedor,id_usuario_actualizacion=@usuario,fecha_actualizacion=now() WHERE id=@id RETURNING {Columnas}"
                : $"INSERT INTO producto(codigo,nombre,costo,precio_venta,categoria_id,empaque_id,proveedor_id,id_usuario_creacion) VALUES(@codigo,@nombre,@costo,@precio,@categoria,@empaque,@proveedor,@usuario) RETURNING {Columnas}";
            await using var cmd=new NpgsqlCommand(sql,conn,tx);
            cmd.Parameters.AddWithValue("codigo",d.Codigo!); cmd.Parameters.AddWithValue("nombre",d.Nombre!);
            cmd.Parameters.AddWithValue("costo",d.Costo); cmd.Parameters.AddWithValue("precio",d.PrecioVenta);
            cmd.Parameters.AddWithValue("categoria",d.CategoriaId); cmd.Parameters.AddWithValue("empaque",d.EmpaqueId);
            cmd.Parameters.AddWithValue("proveedor",d.ProveedorId!); cmd.Parameters.AddWithValue("usuario",usuario);
            if(id.HasValue) cmd.Parameters.AddWithValue("id",id.Value);
            Producto producto;
            await using(var reader=await cmd.ExecuteReaderAsync(ct)) { await reader.ReadAsync(ct); producto=Leer(reader); }
            if(precioAnterior.HasValue && precioAnterior!=d.PrecioVenta) {
                await using var hist=new NpgsqlCommand("INSERT INTO historial_precio(producto_id,precio_anterior,precio_nuevo,motivo,usuario_id) VALUES(@id,@anterior,@nuevo,@motivo,@usuario)",conn,tx);
                hist.Parameters.AddWithValue("id",producto.Id); hist.Parameters.AddWithValue("anterior",precioAnterior.Value);
                hist.Parameters.AddWithValue("nuevo",d.PrecioVenta); hist.Parameters.AddWithValue("motivo",d.MotivoCambioPrecio!);
                hist.Parameters.AddWithValue("usuario",usuario); await hist.ExecuteNonQueryAsync(ct);
            }
            await tx.CommitAsync(ct); return Result.Success(producto);
        } catch(Exception e) when(Tecnico(e)) { return Result.Failure<Producto>(Traducir(e)); }
    }
    public async Task<Result> DesactivarAsync(int id,string usuario,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(usuario)) return Result.Failure(Error.Unauthorized("Usuario.NoAutenticado","Se requiere una identidad autenticada."));
        try {
            await using var conn=await source.OpenConnectionAsync(ct);
            await using var tx=await conn.BeginTransactionAsync(ct);
            await using var cmd=new NpgsqlCommand("SELECT esta_activo FROM producto WHERE id=@id FOR UPDATE",conn,tx);
            cmd.Parameters.AddWithValue("id",id);
            var estado=await cmd.ExecuteScalarAsync(ct);
            if(estado is null) return Result.Failure(NoExiste);
            if(!(bool)estado) return Result.Failure(Inactivo);
            await using var update=new NpgsqlCommand("UPDATE producto SET esta_activo=false,id_usuario_actualizacion=@usuario,fecha_actualizacion=now() WHERE id=@id",conn,tx);
            update.Parameters.AddWithValue("usuario",usuario); update.Parameters.AddWithValue("id",id);
            await update.ExecuteNonQueryAsync(ct); await tx.CommitAsync(ct); return Result.Success();
        } catch(Exception e) when(Tecnico(e)) { return Result.Failure(Traducir(e)); }
    }
}
