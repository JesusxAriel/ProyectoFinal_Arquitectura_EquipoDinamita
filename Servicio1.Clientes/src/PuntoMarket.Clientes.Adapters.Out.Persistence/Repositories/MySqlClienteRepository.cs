using MySqlConnector;
using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Entities;
using PuntoMarket.Clientes.Domain.Errors;

namespace PuntoMarket.Clientes.Adapters.Out.Persistence.Repositories;

// =====================================================
// PATRÓN DE DISEÑO 3/4: ADAPTER
// IClienteRepository es el PUERTO que define Application (lenguaje del negocio).
// MySqlClienteRepository lo ADAPTA a MySQL: traduce Cliente <-> filas SQL, filtros, orden y paginación
// a SQL parametrizado, y el error 1062 de MySQL a un Result.Failure(Conflict). El dominio no conoce MySQL.
// =====================================================
public sealed class MySqlClienteRepository(MySqlDataSource dataSource) : IClienteRepository
{
    private const int ErrorEntradaDuplicada = 1062;

    private const string Columnas =
        "id, nitCi, razonSocial, correo, telefono, estaActivo, idUsuarioCreacion, idUsuarioActualizacion, fechaCreacion, fechaActualizacion";

    public async Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken)
    {
        await using var conexion = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var comando = new MySqlCommand($"SELECT {Columnas} FROM CLIENTE WHERE id = @id", conexion);
        comando.Parameters.AddWithValue("@id", id);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        return await lector.ReadAsync(cancellationToken) ? Mapear(lector) : null;
    }

    public async Task<PaginaClientes> ListarAsync(ListarClientesQuery consulta, CancellationToken cancellationToken)
    {
        var where = ConstruirWhere(consulta);
        await using var conexion = await dataSource.OpenConnectionAsync(cancellationToken);

        long total;
        await using (var conteo = new MySqlCommand($"SELECT COUNT(*) FROM CLIENTE{where}", conexion))
        {
            AgregarParametrosDeFiltro(conteo, consulta);
            total = Convert.ToInt64(await conteo.ExecuteScalarAsync(cancellationToken));
        }

        // Ordena ANTES de paginar y desempata por id para que la paginación sea estable.
        var sql = $"SELECT {Columnas} FROM CLIENTE{where} ORDER BY {ColumnaDeOrden(consulta.OrdenarPor)} {(consulta.Descendente ? "DESC" : "ASC")}, id ASC LIMIT @limite OFFSET @desplazamiento";
        await using var listado = new MySqlCommand(sql, conexion);
        AgregarParametrosDeFiltro(listado, consulta);
        listado.Parameters.AddWithValue("@limite", consulta.TamanoPagina);
        listado.Parameters.AddWithValue("@desplazamiento", consulta.Desplazamiento);

        var items = new List<Cliente>();
        await using var lector = await listado.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken)) items.Add(Mapear(lector));

        return new PaginaClientes(items, total);
    }

    public async Task<bool> ExisteActivoConNitCiAsync(string nitCi, int? excluirId, CancellationToken cancellationToken)
    {
        var sql = "SELECT 1 FROM CLIENTE WHERE estaActivo = 1 AND nitCi = @nitCi"
                  + (excluirId.HasValue ? " AND id <> @excluirId" : string.Empty) + " LIMIT 1";

        await using var conexion = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@nitCi", nitCi);
        if (excluirId.HasValue) comando.Parameters.AddWithValue("@excluirId", excluirId.Value);

        return await comando.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<Result<Cliente>> AgregarAsync(Cliente cliente, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO CLIENTE (nitCi, razonSocial, correo, telefono, estaActivo, idUsuarioCreacion, fechaCreacion)
                             VALUES (@nitCi, @razonSocial, @correo, @telefono, @estaActivo, @idUsuarioCreacion, @fechaCreacion)";
        try
        {
            await using var conexion = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@nitCi", cliente.NitCi);
            comando.Parameters.AddWithValue("@razonSocial", cliente.RazonSocial);
            comando.Parameters.AddWithValue("@correo", (object?)cliente.Correo ?? DBNull.Value);
            comando.Parameters.AddWithValue("@telefono", (object?)cliente.Telefono ?? DBNull.Value);
            comando.Parameters.AddWithValue("@estaActivo", cliente.EstaActivo);
            comando.Parameters.AddWithValue("@idUsuarioCreacion", cliente.IdUsuarioCreacion);
            comando.Parameters.AddWithValue("@fechaCreacion", cliente.FechaCreacion);

            await comando.ExecuteNonQueryAsync(cancellationToken);
            cliente.AsignarId(checked((int)comando.LastInsertedId));
            return Result.Success(cliente);
        }
        catch (MySqlException ex) when (ex.Number == ErrorEntradaDuplicada)
        {
            return Result.Failure<Cliente>(ClienteErrors.NitCiDuplicado);
        }
    }

    public async Task<Result> ActualizarAsync(Cliente cliente, CancellationToken cancellationToken)
    {
        const string sql = @"UPDATE CLIENTE
                             SET nitCi = @nitCi, razonSocial = @razonSocial, correo = @correo, telefono = @telefono,
                                 estaActivo = @estaActivo, idUsuarioActualizacion = @idUsuarioActualizacion,
                                 fechaActualizacion = @fechaActualizacion
                             WHERE id = @id";
        try
        {
            await using var conexion = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", cliente.Id);
            comando.Parameters.AddWithValue("@nitCi", cliente.NitCi);
            comando.Parameters.AddWithValue("@razonSocial", cliente.RazonSocial);
            comando.Parameters.AddWithValue("@correo", (object?)cliente.Correo ?? DBNull.Value);
            comando.Parameters.AddWithValue("@telefono", (object?)cliente.Telefono ?? DBNull.Value);
            comando.Parameters.AddWithValue("@estaActivo", cliente.EstaActivo);
            comando.Parameters.AddWithValue("@idUsuarioActualizacion", (object?)cliente.IdUsuarioActualizacion ?? DBNull.Value);
            comando.Parameters.AddWithValue("@fechaActualizacion", (object?)cliente.FechaActualizacion ?? DBNull.Value);

            // MySqlConnector devuelve las filas ENCONTRADAS (no solo las cambiadas): 0 significa que el id ya no existe.
            var filas = await comando.ExecuteNonQueryAsync(cancellationToken);
            return filas == 0 ? Result.Failure(ClienteErrors.NoEncontrado(cliente.Id)) : Result.Success();
        }
        catch (MySqlException ex) when (ex.Number == ErrorEntradaDuplicada)
        {
            return Result.Failure(ClienteErrors.NitCiDuplicado);
        }
    }

    // ---------- Construcción de SQL (siempre parametrizado; las columnas de orden salen de una lista blanca) ----------

    private static string ConstruirWhere(ListarClientesQuery consulta)
    {
        var condiciones = new List<string>();

        if (consulta.Estado == EstadoFiltro.Activos) condiciones.Add("estaActivo = 1");
        else if (consulta.Estado == EstadoFiltro.Inactivos) condiciones.Add("estaActivo = 0");

        if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            condiciones.Add("(razonSocial LIKE @busqueda OR nitCi LIKE @busqueda OR correo LIKE @busqueda OR telefono LIKE @busqueda)");

        return condiciones.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", condiciones);
    }

    private static void AgregarParametrosDeFiltro(MySqlCommand comando, ListarClientesQuery consulta)
    {
        if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            comando.Parameters.AddWithValue("@busqueda", "%" + EscaparLike(consulta.Busqueda) + "%");
    }

    /// <summary>Los comodines de LIKE (% _ \) escritos por el usuario se tratan como texto literal.</summary>
    private static string EscaparLike(string texto) =>
        texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static string ColumnaDeOrden(CampoOrden campo) => campo switch
    {
        CampoOrden.NitCi => "nitCi",
        CampoOrden.FechaCreacion => "fechaCreacion",
        CampoOrden.FechaActualizacion => "fechaActualizacion",
        _ => "razonSocial"
    };

    private static Cliente Mapear(MySqlDataReader l) => Cliente.Rehidratar(
        id: l.GetInt32(0),
        nitCi: l.GetString(1),
        razonSocial: l.GetString(2),
        correo: l.IsDBNull(3) ? null : l.GetString(3),
        telefono: l.IsDBNull(4) ? null : l.GetString(4),
        estaActivo: l.GetBoolean(5),
        idUsuarioCreacion: l.GetString(6),
        idUsuarioActualizacion: l.IsDBNull(7) ? null : l.GetString(7),
        fechaCreacion: DateTime.SpecifyKind(l.GetDateTime(8), DateTimeKind.Utc),
        fechaActualizacion: l.IsDBNull(9) ? null : DateTime.SpecifyKind(l.GetDateTime(9), DateTimeKind.Utc));
}
