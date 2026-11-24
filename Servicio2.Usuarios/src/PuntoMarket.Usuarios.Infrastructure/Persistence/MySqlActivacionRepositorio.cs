using MySql.Data.MySqlClient;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Infrastructure.Persistence;

public sealed class MySqlActivacionRepositorio(ConexionMySql conexionMySql) : IActivacionRepositorio
{
    public const string UsuarioInexistente = "El usuario ya no existe.";

    public async Task<Result> GuardarToken(
        int usuarioId,
        string hash,
        DateTime expiraUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(hash);

        const string sql = """
            UPDATE USUARIO
            SET tokenActivacionHash = @hash,
                tokenActivacionExpira = @expira
            WHERE id = @id;
            """;

        await using var conexion = await AbrirConexion(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@hash", hash);
        comando.Parameters.AddWithValue("@expira", expiraUtc);
        comando.Parameters.AddWithValue("@id", usuarioId);

        return await comando.ExecuteNonQueryAsync(cancellationToken) == 0
            ? Result.Fail(UsuarioInexistente)
            : Result.Ok();
    }

    public async Task<TokenConsumido?> ConsumirToken(
        string hash,
        DateTime ahoraUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(hash);

        var seleccionar = $"""
            SELECT {MySqlUsuarioRepositorio.ColumnasUsuario}, tokenActivacionExpira
            FROM USUARIO
            WHERE tokenActivacionHash = @hash
              AND tokenActivacionExpira > @ahora
              AND activo = 1
            FOR UPDATE;
            """;

        const string anular = """
            UPDATE USUARIO
            SET tokenActivacionHash = NULL,
                tokenActivacionExpira = NULL
            WHERE id = @id;
            """;

        await using var conexion = await AbrirConexion(cancellationToken);
        await using var transaccion = await conexion.BeginTransactionAsync(cancellationToken);

        TokenConsumido consumido;
        await using (var consulta = new MySqlCommand(seleccionar, conexion, transaccion))
        {
            consulta.Parameters.AddWithValue("@hash", hash);
            consulta.Parameters.AddWithValue("@ahora", ahoraUtc);
            await using var lector = await consulta.ExecuteReaderAsync(cancellationToken);

            if (!await lector.ReadAsync(cancellationToken))
                return null;

            consumido = new TokenConsumido(
                MySqlUsuarioRepositorio.LeerUsuario(lector),
                DateTime.SpecifyKind(lector.GetDateTime(11), DateTimeKind.Utc));
        }

        await using (var actualizacion = new MySqlCommand(anular, conexion, transaccion))
        {
            actualizacion.Parameters.AddWithValue("@id", consumido.Usuario.Id);
            await actualizacion.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaccion.CommitAsync(cancellationToken);
        return consumido;
    }

    public async Task RestaurarToken(
        int usuarioId,
        string hash,
        DateTime expiraUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(hash);

        const string sql = """
            UPDATE USUARIO
            SET tokenActivacionHash = @hash,
                tokenActivacionExpira = @expira
            WHERE id = @id
              AND tokenActivacionHash IS NULL;
            """;

        await using var conexion = await AbrirConexion(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@hash", hash);
        comando.Parameters.AddWithValue("@expira", expiraUtc);
        comando.Parameters.AddWithValue("@id", usuarioId);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<MySqlConnection> AbrirConexion(CancellationToken cancellationToken)
    {
        var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        return conexion;
    }
}
