using MySqlConnector;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Entities;
using PuntoMarket.Clientes.Domain.Errors;

namespace PuntoMarket.Clientes.Adapters.Out.Persistence.Repositories;

// PATRÓN DE DISEÑO 3/4: Adapter
// Traduce el puerto IClienteRepository a SQL MySQL parametrizado.
public sealed class MySqlClienteRepository(MySqlDataSource dataSource) : IClienteRepository
{
    public async Task<Cliente?> ObtenerAsync(int id, CancellationToken ct)
    {
        await using var conexion = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conexion.CreateCommand();
        cmd.CommandText = "SELECT id,nitCi,razonSocial,correo,telefono,estaActivo,nitCi='0' AS esGenerico,idUsuarioCreacion,idUsuarioActualizacion,fechaCreacion,fechaActualizacion FROM CLIENTE WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Leer(reader) : null;
    }

    public async Task<(IReadOnlyList<Cliente> Items, long Total)> ListarAsync(ConsultaClientes consulta, CancellationToken ct)
    {
        var estado = consulta.Estado.Equals("Activos", StringComparison.OrdinalIgnoreCase) ? " AND estaActivo=1"
            : consulta.Estado.Equals("Inactivos", StringComparison.OrdinalIgnoreCase) ? " AND estaActivo=0" : "";
        var busqueda = string.IsNullOrWhiteSpace(consulta.Busqueda) ? "" : " AND (razonSocial LIKE @busqueda ESCAPE '\\\\' OR nitCi LIKE @busqueda ESCAPE '\\\\')";
        var columna = consulta.OrdenarPor switch { "NitCi" => "nitCi", "FechaCreacion" => "fechaCreacion", "FechaActualizacion" => "fechaActualizacion", _ => "razonSocial" };
        var direccion = consulta.Descendente ? "DESC" : "ASC";
        await using var conexion = await dataSource.OpenConnectionAsync(ct);
        await using var count = conexion.CreateCommand();
        count.CommandText = $"SELECT COUNT(*) FROM CLIENTE WHERE 1=1{estado}{busqueda}";
        if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            count.Parameters.AddWithValue("@busqueda", $"%{Escapar(consulta.Busqueda)}%");
        var total = Convert.ToInt64(await count.ExecuteScalarAsync(ct));
        await using var cmd = conexion.CreateCommand();
        cmd.CommandText = $"SELECT id,nitCi,razonSocial,correo,telefono,estaActivo,nitCi='0' AS esGenerico,idUsuarioCreacion,idUsuarioActualizacion,fechaCreacion,fechaActualizacion FROM CLIENTE WHERE 1=1{estado}{busqueda} ORDER BY {columna} {direccion}, id ASC LIMIT @tamano OFFSET @offset";
        if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            cmd.Parameters.AddWithValue("@busqueda", $"%{Escapar(consulta.Busqueda)}%");
        cmd.Parameters.AddWithValue("@tamano", consulta.TamanoPagina);
        cmd.Parameters.AddWithValue("@offset", (consulta.Pagina - 1) * consulta.TamanoPagina);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var items = new List<Cliente>();
        while (await reader.ReadAsync(ct)) items.Add(Leer(reader));
        return (items, total);
    }

    public async Task<Result<Cliente>> CrearAsync(Cliente cliente, CancellationToken ct)
    {
        await using var conexion = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conexion.CreateCommand();
        cmd.CommandText = "INSERT INTO CLIENTE(nitCi,razonSocial,correo,telefono,estaActivo,idUsuarioCreacion,fechaCreacion) VALUES(@nit,@razon,@correo,@telefono,1,@usuario,@fecha); SELECT LAST_INSERT_ID();";
        cmd.Parameters.AddWithValue("@nit", cliente.Datos.NitCi);
        cmd.Parameters.AddWithValue("@razon", cliente.Datos.RazonSocial);
        cmd.Parameters.AddWithValue("@correo", (object?)cliente.Datos.Correo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@telefono", (object?)cliente.Datos.Telefono ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@usuario", cliente.IdUsuarioCreacion);
        cmd.Parameters.AddWithValue("@fecha", cliente.FechaCreacion);
        try
        {
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            return Result.Success(Cliente.Crear(id, cliente.Datos, true, cliente.EsGenerico, cliente.IdUsuarioCreacion,
                null, cliente.FechaCreacion, null));
        }
        catch (MySqlException e) when (e.Number == 1062)
        {
            return Result.Failure<Cliente>(ClienteErrors.NitCiDuplicado);
        }
    }

    public async Task<Result<Cliente>> ActualizarAsync(Cliente cliente, CancellationToken ct)
    {
        await using var conexion = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conexion.CreateCommand();
        cmd.CommandText = "UPDATE CLIENTE SET nitCi=@nit,razonSocial=@razon,correo=@correo,telefono=@telefono,idUsuarioActualizacion=@usuario,fechaActualizacion=@fecha WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", cliente.Id);
        cmd.Parameters.AddWithValue("@nit", cliente.Datos.NitCi);
        cmd.Parameters.AddWithValue("@razon", cliente.Datos.RazonSocial);
        cmd.Parameters.AddWithValue("@correo", (object?)cliente.Datos.Correo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@telefono", (object?)cliente.Datos.Telefono ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@usuario", cliente.IdUsuarioActualizacion);
        cmd.Parameters.AddWithValue("@fecha", cliente.FechaActualizacion);
        try { await cmd.ExecuteNonQueryAsync(ct); return Result.Success(cliente); }
        catch (MySqlException e) when (e.Number == 1062) { return Result.Failure<Cliente>(ClienteErrors.NitCiDuplicado); }
    }

    public async Task<Result> DesactivarAsync(Cliente cliente, string usuario, DateTime fecha, CancellationToken ct)
    {
        await using var conexion = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conexion.CreateCommand();
        cmd.CommandText = "UPDATE CLIENTE SET estaActivo=0,idUsuarioActualizacion=@usuario,fechaActualizacion=@fecha WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", cliente.Id);
        cmd.Parameters.AddWithValue("@usuario", usuario);
        cmd.Parameters.AddWithValue("@fecha", fecha);
        await cmd.ExecuteNonQueryAsync(ct);
        return Result.Success();
    }

    public async Task<Result<Cliente>> ReactivarAsync(Cliente cliente, string usuario, DateTime fecha, CancellationToken ct)
    {
        await using var conexion = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conexion.CreateCommand();
        cmd.CommandText = "UPDATE CLIENTE SET estaActivo=1,idUsuarioActualizacion=@usuario,fechaActualizacion=@fecha WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", cliente.Id);
        cmd.Parameters.AddWithValue("@usuario", usuario);
        cmd.Parameters.AddWithValue("@fecha", fecha);
        try { await cmd.ExecuteNonQueryAsync(ct); return Result.Success(cliente); }
        catch (MySqlException e) when (e.Number == 1062) { return Result.Failure<Cliente>(ClienteErrors.NoReactivar); }
    }

    public async Task<bool> ExisteNitCiActivoAsync(string nitCi, int? excluirId, CancellationToken ct)
    {
        await using var conexion = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = conexion.CreateCommand();
        cmd.CommandText = excluirId is null
            ? "SELECT EXISTS(SELECT 1 FROM CLIENTE WHERE nitCi=@nit AND estaActivo=1)"
            : "SELECT EXISTS(SELECT 1 FROM CLIENTE WHERE nitCi=@nit AND estaActivo=1 AND id<>@id)";
        cmd.Parameters.AddWithValue("@nit", nitCi);
        if (excluirId is not null) cmd.Parameters.AddWithValue("@id", excluirId.Value);
        return Convert.ToBoolean(await cmd.ExecuteScalarAsync(ct));
    }

    private static string Escapar(string texto) => texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    private static Cliente Leer(MySqlDataReader r) => Cliente.Crear(r.GetInt32(0),
        new DatosCliente(r.GetString(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4)),
        r.GetBoolean(5), r.GetBoolean(6), r.GetString(7), r.IsDBNull(8) ? null : r.GetString(8),
        r.GetDateTime(9), r.IsDBNull(10) ? null : r.GetDateTime(10));
}
