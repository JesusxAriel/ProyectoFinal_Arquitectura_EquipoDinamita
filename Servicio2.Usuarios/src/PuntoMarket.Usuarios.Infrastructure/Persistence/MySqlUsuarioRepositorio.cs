using System.Data.Common;
using MySql.Data.MySqlClient;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;

namespace PuntoMarket.Usuarios.Infrastructure.Persistence;

public sealed class MySqlUsuarioRepositorio(ConexionMySql conexionMySql) : IUsuarioRepositorio
{
    public const string UsernameDuplicado = "Ya existe un usuario con ese nombre de usuario.";
    public const string CorreoDuplicado = "Ya existe un usuario con ese correo.";
    public const string RolInexistente = "El rol indicado no existe.";
    public const string UsuarioInexistente = "El usuario ya no existe.";
    public const string DatosDuplicados = "Ya existe un usuario con esos datos.";

    private const int CodigoDuplicado = 1062;
    private const int CodigoLlaveForanea = 1452;
    private const string IndiceUsername = "uq_usuario_username";
    private const string IndiceCorreo = "uq_usuario_correo";

    private const string ColumnasUsuario =
        "id, username, correo, nombres, apellidoPaterno, apellidoMaterno, idRol, activo, debeCambiarPassword, " +
        "usuarioCreacion, fechaCreacion, usuarioModificacion, fechaModificacion";

    private const string ColumnasRol = "id, nombre, estaActivo";

    public async Task<Result<Usuario>> Guardar(
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        const string sql = """
            INSERT INTO USUARIO
                (username, correo, nombres, apellidoPaterno, apellidoMaterno, idRol, activo,
                 debeCambiarPassword, usuarioCreacion, fechaCreacion)
            VALUES
                (@username, @correo, @nombres, @apellidoPaterno, @apellidoMaterno, @idRol, @activo,
                 @debeCambiarPassword, @usuarioCreacion, @fechaCreacion);
            SELECT LAST_INSERT_ID();
            """;

        await using var conexion = await AbrirConexion(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@username", usuario.Username);
        comando.Parameters.AddWithValue("@correo", usuario.Correo);
        comando.Parameters.AddWithValue("@nombres", usuario.Nombres);
        comando.Parameters.AddWithValue("@apellidoPaterno", usuario.ApellidoPaterno);
        comando.Parameters.AddWithValue("@apellidoMaterno", Valor(usuario.ApellidoMaterno));
        comando.Parameters.AddWithValue("@idRol", usuario.RolId);
        comando.Parameters.AddWithValue("@activo", usuario.Activo);
        comando.Parameters.AddWithValue("@debeCambiarPassword", usuario.DebeCambiarPassword);
        comando.Parameters.AddWithValue("@usuarioCreacion", usuario.UsuarioCreacion);
        comando.Parameters.AddWithValue("@fechaCreacion", usuario.FechaCreacion);

        try
        {
            var id = Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken));
            return Result<Usuario>.Ok(usuario.ConIdAsignado(id));
        }
        catch (MySqlException excepcion) when (EsConflicto(excepcion))
        {
            return Result<Usuario>.Fail(MensajeDeConflicto(excepcion));
        }
    }

    public async Task<Result<Usuario>> Actualizar(
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        const string sql = """
            UPDATE USUARIO
            SET correo = @correo,
                nombres = @nombres,
                apellidoPaterno = @apellidoPaterno,
                apellidoMaterno = @apellidoMaterno,
                idRol = @idRol,
                activo = @activo,
                debeCambiarPassword = @debeCambiarPassword,
                usuarioModificacion = @usuarioModificacion,
                fechaModificacion = @fechaModificacion
            WHERE id = @id;
            """;

        await using var conexion = await AbrirConexion(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@correo", usuario.Correo);
        comando.Parameters.AddWithValue("@nombres", usuario.Nombres);
        comando.Parameters.AddWithValue("@apellidoPaterno", usuario.ApellidoPaterno);
        comando.Parameters.AddWithValue("@apellidoMaterno", Valor(usuario.ApellidoMaterno));
        comando.Parameters.AddWithValue("@idRol", usuario.RolId);
        comando.Parameters.AddWithValue("@activo", usuario.Activo);
        comando.Parameters.AddWithValue("@debeCambiarPassword", usuario.DebeCambiarPassword);
        comando.Parameters.AddWithValue("@usuarioModificacion", Valor(usuario.UsuarioModificacion));
        comando.Parameters.AddWithValue("@fechaModificacion", Valor(usuario.FechaModificacion));
        comando.Parameters.AddWithValue("@id", usuario.Id);

        try
        {
            return await comando.ExecuteNonQueryAsync(cancellationToken) == 0
                ? Result<Usuario>.Fail(UsuarioInexistente)
                : Result<Usuario>.Ok(usuario);
        }
        catch (MySqlException excepcion) when (EsConflicto(excepcion))
        {
            return Result<Usuario>.Fail(MensajeDeConflicto(excepcion));
        }
    }

    public Task<Usuario?> ObtenerPorId(int id, CancellationToken cancellationToken = default) =>
        ObtenerUno($"SELECT {ColumnasUsuario} FROM USUARIO WHERE id = @valor;", id, cancellationToken);

    public Task<Usuario?> ObtenerPorUsername(string username, CancellationToken cancellationToken = default) =>
        ObtenerUno($"SELECT {ColumnasUsuario} FROM USUARIO WHERE username = @valor;", username, cancellationToken);

    public async Task<IReadOnlyList<Usuario>> Listar(CancellationToken cancellationToken = default)
    {
        await using var conexion = await AbrirConexion(cancellationToken);
        await using var comando = new MySqlCommand(
            $"SELECT {ColumnasUsuario} FROM USUARIO ORDER BY username;", conexion);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);

        var usuarios = new List<Usuario>();
        while (await lector.ReadAsync(cancellationToken))
            usuarios.Add(LeerUsuario(lector));

        return usuarios;
    }

    public Task<bool> ExisteUsername(string username, CancellationToken cancellationToken = default) =>
        Existe("SELECT EXISTS(SELECT 1 FROM USUARIO WHERE username = @valor);", username, cancellationToken);

    public Task<bool> ExisteCorreo(string correo, CancellationToken cancellationToken = default) =>
        Existe("SELECT EXISTS(SELECT 1 FROM USUARIO WHERE correo = @valor);", correo, cancellationToken);

    public async Task<Rol?> ObtenerRol(int rolId, CancellationToken cancellationToken = default)
    {
        await using var conexion = await AbrirConexion(cancellationToken);
        await using var comando = new MySqlCommand(
            $"SELECT {ColumnasRol} FROM ROL WHERE id = @valor;", conexion);
        comando.Parameters.AddWithValue("@valor", rolId);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);

        return await lector.ReadAsync(cancellationToken) ? LeerRol(lector) : null;
    }

    public Task<IReadOnlyList<Rol>> ListarRoles(CancellationToken cancellationToken = default) =>
        ListarRoles($"SELECT {ColumnasRol} FROM ROL ORDER BY nombre;", cancellationToken);

    public Task<IReadOnlyList<Rol>> ListarRolesActivos(CancellationToken cancellationToken = default) =>
        ListarRoles(
            $"SELECT {ColumnasRol} FROM ROL WHERE estaActivo = 1 ORDER BY nombre;", cancellationToken);

    private async Task<IReadOnlyList<Rol>> ListarRoles(string sql, CancellationToken cancellationToken)
    {
        await using var conexion = await AbrirConexion(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);

        var roles = new List<Rol>();
        while (await lector.ReadAsync(cancellationToken))
            roles.Add(LeerRol(lector));

        return roles;
    }

    private async Task<Usuario?> ObtenerUno(string sql, object valor, CancellationToken cancellationToken)
    {
        await using var conexion = await AbrirConexion(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@valor", valor);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);

        return await lector.ReadAsync(cancellationToken) ? LeerUsuario(lector) : null;
    }

    private async Task<bool> Existe(string sql, object valor, CancellationToken cancellationToken)
    {
        await using var conexion = await AbrirConexion(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@valor", valor);

        return Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    private async Task<MySqlConnection> AbrirConexion(CancellationToken cancellationToken)
    {
        var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        return conexion;
    }

    private static Usuario LeerUsuario(DbDataReader lector) =>
        Usuario.Reconstruir(
            lector.GetInt32(0),
            lector.GetString(1),
            lector.GetString(2),
            lector.GetString(3),
            lector.GetString(4),
            lector.IsDBNull(5) ? null : lector.GetString(5),
            lector.GetInt32(6),
            lector.GetBoolean(7),
            lector.GetBoolean(8),
            lector.GetString(9),
            EnUtc(lector.GetDateTime(10)),
            lector.IsDBNull(11) ? null : lector.GetString(11),
            lector.IsDBNull(12) ? null : EnUtc(lector.GetDateTime(12)));

    private static Rol LeerRol(DbDataReader lector) =>
        Rol.Reconstruir(lector.GetInt32(0), lector.GetString(1), lector.GetBoolean(2));

    private static DateTime EnUtc(DateTime fecha) => DateTime.SpecifyKind(fecha, DateTimeKind.Utc);

    private static object Valor(object? valor) => valor ?? DBNull.Value;

    private static bool EsConflicto(MySqlException excepcion) =>
        excepcion.Number is CodigoDuplicado or CodigoLlaveForanea;

    private static string MensajeDeConflicto(MySqlException excepcion)
    {
        if (excepcion.Number == CodigoLlaveForanea)
            return RolInexistente;

        if (excepcion.Message.Contains(IndiceCorreo, StringComparison.OrdinalIgnoreCase))
            return CorreoDuplicado;

        return excepcion.Message.Contains(IndiceUsername, StringComparison.OrdinalIgnoreCase)
            ? UsernameDuplicado
            : DatosDuplicados;
    }
}
