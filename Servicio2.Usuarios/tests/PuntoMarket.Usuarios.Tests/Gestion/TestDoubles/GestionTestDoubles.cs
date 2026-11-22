using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;

namespace PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;

internal sealed class RelojFijo : IReloj
{
    public DateTime Ahora { get; } = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
}

internal sealed record UsuarioActualStub(string Username = "admin.test") : IUsuarioActual;

internal sealed class GestorDeIdentidadFalso : IGestorDeIdentidad
{
    public Result ResultadoCrearUsuario { get; set; } = Result.Ok();
    public Result ResultadoVerificarPassword { get; set; } = Result.Ok();
    public Result ResultadoCambiarPassword { get; set; } = Result.Ok();
    public Result ResultadoCambiarEstado { get; set; } = Result.Ok();
    public List<string> Llamadas { get; } = [];
    public List<(string Username, string Password)> Verificaciones { get; } = [];
    public List<(string Username, string Password)> Cambios { get; } = [];

    public Task<Result> CrearUsuario(
        string username, string correo, string nombreCompleto, string rol, string passwordTemporal)
    {
        Llamadas.Add(nameof(CrearUsuario));
        return Task.FromResult(ResultadoCrearUsuario);
    }

    public Task<Result> VerificarPassword(string username, string passwordActual)
    {
        Llamadas.Add(nameof(VerificarPassword));
        Verificaciones.Add((username, passwordActual));
        return Task.FromResult(ResultadoVerificarPassword);
    }

    public Task<Result> CambiarPassword(string username, string passwordNueva)
    {
        Llamadas.Add(nameof(CambiarPassword));
        Cambios.Add((username, passwordNueva));
        return Task.FromResult(ResultadoCambiarPassword);
    }

    public Task<Result> CambiarEstado(string username, bool activo)
    {
        Llamadas.Add(nameof(CambiarEstado));
        return Task.FromResult(ResultadoCambiarEstado);
    }
}

internal sealed class UsuarioRepositorioStub : IUsuarioRepositorio
{
    public Usuario? UsuarioEncontrado { get; set; }
    public Rol? RolEncontrado { get; set; }
    public Result<Usuario>? ResultadoActualizar { get; set; }
    public Result<Usuario>? ResultadoGuardar { get; set; }
    public int IdAsignado { get; set; } = 42;
    public bool CorreoExistente { get; set; }
    public HashSet<string> UsernamesExistentes { get; } = [];
    public List<Usuario> Actualizados { get; } = [];
    public List<Usuario> Guardados { get; } = [];
    public List<string> UsernamesVerificados { get; } = [];
    public List<string> UsernamesConsultados { get; } = [];
    public List<int> RolesConsultados { get; } = [];

    public Task<Usuario?> ObtenerPorUsername(string username, CancellationToken cancellationToken = default)
    {
        UsernamesConsultados.Add(username);
        return Task.FromResult(UsuarioEncontrado);
    }

    public Task<Rol?> ObtenerRol(int rolId, CancellationToken cancellationToken = default)
    {
        RolesConsultados.Add(rolId);
        return Task.FromResult(RolEncontrado);
    }

    public Task<Result<Usuario>> Guardar(Usuario usuario, CancellationToken cancellationToken = default)
    {
        Guardados.Add(usuario);
        return Task.FromResult(ResultadoGuardar ?? Result<Usuario>.Ok(usuario.ConIdAsignado(IdAsignado)));
    }

    public Task<Result<Usuario>> Actualizar(Usuario usuario, CancellationToken cancellationToken = default)
    {
        Actualizados.Add(usuario);
        return Task.FromResult(ResultadoActualizar ?? Result<Usuario>.Ok(usuario));
    }

    public Task<Usuario?> ObtenerPorId(int id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<Usuario>> Listar(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> ExisteUsername(string username, CancellationToken cancellationToken = default)
    {
        UsernamesVerificados.Add(username);
        return Task.FromResult(UsernamesExistentes.Contains(username));
    }

    public Task<bool> ExisteCorreo(string correo, CancellationToken cancellationToken = default) =>
        Task.FromResult(CorreoExistente);

    public Task<IReadOnlyList<Rol>> ListarRolesActivos(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal sealed class EnviadorCorreoSpy : IEnviadorCorreo
{
    public Result Resultado { get; set; } = Result.Ok();
    public Exception? ExcepcionAlEnviar { get; set; }
    public List<(string Correo, string Username, string PasswordTemporal)> Envios { get; } = [];

    public Task<Result> EnviarCredenciales(
        string correo, string username, string passwordTemporal,
        CancellationToken cancellationToken = default)
    {
        Envios.Add((correo, username, passwordTemporal));

        return ExcepcionAlEnviar is null
            ? Task.FromResult(Resultado)
            : throw ExcepcionAlEnviar;
    }
}

internal sealed class GeneradorPasswordFalso(string password = "Temporal#2026") : IGeneradorPassword
{
    public int Llamadas { get; private set; }

    public string Password => password;

    public string Generar()
    {
        Llamadas++;
        return password;
    }
}

internal sealed class UsuarioGestionRepositorioStub : IUsuarioGestionRepositorio
{
    public bool ExisteCorreo { get; set; }
    public bool RolActivo { get; set; } = true;
    public HashSet<string> UsernamesExistentes { get; } = [];
    public List<string> UsernamesConsultados { get; } = [];
    public List<UsuarioAltaInterna> Altas { get; } = [];
    public int IdCreado { get; set; } = 42;
    public bool FallarCreacion { get; set; }
    public IReadOnlyList<UsuarioListadoDto> Usuarios { get; set; } = [];
    public UsuarioActivacionDto? UsuarioParaActivacion { get; set; }
    public string? CorreoRenovacion { get; set; } = "ana@example.com";
    public bool ResultadoDesactivacion { get; set; } = true;
    public bool ResultadoTokenValido { get; set; } = true;
    public Dictionary<string, DateTime>? TokensVigentes { get; set; }
    public bool ResultadoActivacion { get; set; } = true;
    public Action? AlActivar { get; set; }
    public List<string> Llamadas { get; } = [];
    public List<(int UsuarioId, string Hash, DateTime Expira, int IdUsuarioActualizacion)> Renovaciones { get; } = [];
    public List<(int UsuarioId, int IdUsuarioEliminacion)> Bajas { get; } = [];
    public List<(string Hash, DateTime Ahora)> Validaciones { get; } = [];
    public List<(string TokenHash, string PasswordHash, DateTime Ahora)> Activaciones { get; } = [];

    public Task<IReadOnlyList<UsuarioListadoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(ListarAsync));
        return Task.FromResult(Usuarios);
    }

    public Task<IReadOnlyList<RolActivoDto>> ListarRolesActivosAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> ExisteUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(ExisteUsernameAsync));
        UsernamesConsultados.Add(username);
        return Task.FromResult(UsernamesExistentes.Contains(username));
    }

    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(ExisteCorreoAsync));
        return Task.FromResult(ExisteCorreo);
    }

    public Task<bool> EsRolActivoAsync(int idRol, CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(EsRolActivoAsync));
        return Task.FromResult(RolActivo);
    }

    public Task<int> CrearAsync(UsuarioAltaInterna usuario, CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(CrearAsync));
        Altas.Add(usuario);
        return FallarCreacion
            ? Task.FromException<int>(new InvalidOperationException("Fallo de persistencia simulado."))
            : Task.FromResult(IdCreado);
    }

    public Task<UsuarioActivacionDto?> ObtenerParaActivacionAsync(int usuarioId,
        CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(ObtenerParaActivacionAsync));
        return Task.FromResult(UsuarioParaActivacion);
    }

    public Task<string?> RenovarTokenActivacionAsync(int usuarioId, string tokenHash, DateTime tokenExpiraUtc,
        int idUsuarioActualizacion,
        CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(RenovarTokenActivacionAsync));
        Renovaciones.Add((usuarioId, tokenHash, tokenExpiraUtc, idUsuarioActualizacion));
        return Task.FromResult(CorreoRenovacion);
    }

    public Task<bool> DesactivarAsync(int usuarioId, int idUsuarioEliminacion,
        CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(DesactivarAsync));
        Bajas.Add((usuarioId, idUsuarioEliminacion));
        return Task.FromResult(ResultadoDesactivacion);
    }

    public Task<bool> TokenValidoAsync(string tokenHash, DateTime ahoraUtc,
        CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(TokenValidoAsync));
        Validaciones.Add((tokenHash, ahoraUtc));
        return Task.FromResult(TokensVigentes is null ? ResultadoTokenValido :
            TokensVigentes.TryGetValue(tokenHash, out var expira) && expira > ahoraUtc);
    }

    public Task<bool> ActivarAsync(string tokenHash, string passwordHash, DateTime ahoraUtc,
        CancellationToken cancellationToken = default)
    {
        AlActivar?.Invoke();
        Llamadas.Add(nameof(ActivarAsync));
        Activaciones.Add((tokenHash, passwordHash, ahoraUtc));
        return Task.FromResult(ResultadoActivacion);
    }
}
