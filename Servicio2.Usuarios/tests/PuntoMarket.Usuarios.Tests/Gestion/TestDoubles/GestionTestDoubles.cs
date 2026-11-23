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
    public List<(string Username, bool Activo)> Estados { get; } = [];

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
        Estados.Add((username, activo));
        return Task.FromResult(ResultadoCambiarEstado);
    }
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

internal sealed class UsuarioRepositorioStub : IUsuarioRepositorio
{
    public Usuario? UsuarioEncontrado { get; set; }
    public Rol? RolEncontrado { get; set; }
    public Result<Usuario>? ResultadoActualizar { get; set; }
    public Result<Usuario>? ResultadoGuardar { get; set; }
    public int IdAsignado { get; set; } = 42;
    public bool CorreoExistente { get; set; }
    public HashSet<string> UsernamesExistentes { get; } = [];
    public List<Usuario> Usuarios { get; set; } = [];
    public List<Rol> Roles { get; set; } = [];
    public List<Usuario> Actualizados { get; } = [];
    public List<Usuario> Guardados { get; } = [];
    public List<string> UsernamesVerificados { get; } = [];
    public List<string> UsernamesConsultados { get; } = [];
    public List<int> IdsConsultados { get; } = [];
    public List<int> RolesConsultados { get; } = [];

    public Task<Usuario?> ObtenerPorUsername(string username, CancellationToken cancellationToken = default)
    {
        UsernamesConsultados.Add(username);
        return Task.FromResult(UsuarioEncontrado);
    }

    public Task<Usuario?> ObtenerPorId(int id, CancellationToken cancellationToken = default)
    {
        IdsConsultados.Add(id);
        return Task.FromResult(UsuarioEncontrado);
    }

    public Task<Rol?> ObtenerRol(int rolId, CancellationToken cancellationToken = default)
    {
        RolesConsultados.Add(rolId);
        return Task.FromResult(RolEncontrado);
    }

    public Task<IReadOnlyList<Usuario>> Listar(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Usuario>>(Usuarios);

    public Task<IReadOnlyList<Rol>> ListarRoles(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Rol>>(Roles);

    public Task<IReadOnlyList<Rol>> ListarRolesActivos(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Rol>>(Roles.Where(rol => rol.Activo).ToList());

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

    public Task<bool> ExisteUsername(string username, CancellationToken cancellationToken = default)
    {
        UsernamesVerificados.Add(username);
        return Task.FromResult(UsernamesExistentes.Contains(username));
    }

    public Task<bool> ExisteCorreo(string correo, CancellationToken cancellationToken = default) =>
        Task.FromResult(CorreoExistente);
}
