namespace PuntoMarket.Usuarios.Domain.Entities;

public sealed class Usuario
{
    public const int LongitudMaximaUsername = 50;
    public const int LongitudMaximaNombreCompleto = 150;
    public const int LongitudMaximaCorreo = 150;

    private const int SinAsignar = 0;
    private const string CampoNombres = "El nombre";
    private const string CampoApellidos = "Los apellidos";
    private const string CampoCorreo = "El correo";
    private const string CampoRol = "El rol";
    private const string CampoUsername = "El nombre de usuario";
    private const string CampoResponsable = "El usuario responsable del cambio";

    private Usuario(
        int id,
        string username,
        string correo,
        string nombreCompleto,
        int rolId,
        bool activo,
        bool debeCambiarPassword,
        string usuarioCreacion,
        DateTime fechaCreacion,
        string? usuarioModificacion,
        DateTime? fechaModificacion)
    {
        Id = id;
        Username = username;
        Correo = correo;
        NombreCompleto = nombreCompleto;
        RolId = rolId;
        Activo = activo;
        DebeCambiarPassword = debeCambiarPassword;
        UsuarioCreacion = usuarioCreacion;
        FechaCreacion = fechaCreacion;
        UsuarioModificacion = usuarioModificacion;
        FechaModificacion = fechaModificacion;
    }

    public int Id { get; }

    public string Username { get; }

    public string Correo { get; private set; }

    public string NombreCompleto { get; private set; }

    public int RolId { get; private set; }

    public bool Activo { get; private set; }

    public bool DebeCambiarPassword { get; private set; }

    public string UsuarioCreacion { get; }

    public DateTime FechaCreacion { get; }

    public string? UsuarioModificacion { get; private set; }

    public DateTime? FechaModificacion { get; private set; }

    public static Result<Usuario> Crear(
        string? nombres,
        string? apellidos,
        string? correo,
        int rolId,
        string? username,
        string? usuarioCreacion,
        DateTime ahora)
    {
        var validacion = Validaciones.PrimerError(
            ValidarUsername(username),
            Validaciones.NoVacio(usuarioCreacion, CampoResponsable),
            ValidarDatos(nombres, apellidos, correo, rolId));

        if (validacion.IsFail)
            return Result<Usuario>.Fail(validacion.Error);

        return Result<Usuario>.Ok(new Usuario(
            SinAsignar,
            Texto.NormalizarEspacios(username),
            NormalizarCorreo(correo),
            ComponerNombreCompleto(nombres, apellidos),
            rolId,
            activo: true,
            debeCambiarPassword: true,
            Texto.NormalizarEspacios(usuarioCreacion),
            ahora,
            usuarioModificacion: null,
            fechaModificacion: null));
    }

    public static Usuario Reconstruir(
        int id,
        string username,
        string correo,
        string nombreCompleto,
        int rolId,
        bool activo,
        bool debeCambiarPassword,
        string usuarioCreacion,
        DateTime fechaCreacion,
        string? usuarioModificacion,
        DateTime? fechaModificacion) =>
        new(id, username, correo, nombreCompleto, rolId, activo, debeCambiarPassword,
            usuarioCreacion, fechaCreacion, usuarioModificacion, fechaModificacion);

    public Usuario ConIdAsignado(int id) =>
        new(id, Username, Correo, NombreCompleto, RolId, Activo, DebeCambiarPassword,
            UsuarioCreacion, FechaCreacion, UsuarioModificacion, FechaModificacion);

    public Result Actualizar(
        string? nombres,
        string? apellidos,
        string? correo,
        int rolId,
        string? usuarioModificacion,
        DateTime ahora)
    {
        var validacion = Validaciones.PrimerError(
            Validaciones.NoVacio(usuarioModificacion, CampoResponsable),
            ValidarDatos(nombres, apellidos, correo, rolId));

        if (validacion.IsFail)
            return validacion;

        NombreCompleto = ComponerNombreCompleto(nombres, apellidos);
        Correo = NormalizarCorreo(correo);
        RolId = rolId;
        RegistrarModificacion(usuarioModificacion, ahora);

        return Result.Ok();
    }

    public Result CambiarEstado(bool activo, string? usuarioModificacion, DateTime ahora)
    {
        var validacion = Validaciones.NoVacio(usuarioModificacion, CampoResponsable);
        if (validacion.IsFail)
            return validacion;

        if (Activo == activo)
            return Result.Fail($"El usuario ya se encuentra {EnPalabras(activo)}.");

        Activo = activo;
        RegistrarModificacion(usuarioModificacion, ahora);

        return Result.Ok();
    }

    public Result PasswordCambiada(string? usuarioModificacion, DateTime ahora)
    {
        var validacion = Validaciones.NoVacio(usuarioModificacion, CampoResponsable);
        if (validacion.IsFail)
            return validacion;

        DebeCambiarPassword = false;
        RegistrarModificacion(usuarioModificacion, ahora);

        return Result.Ok();
    }

    public Result AccesoRestablecido(string? usuarioModificacion, DateTime ahora)
    {
        var validacion = Validaciones.NoVacio(usuarioModificacion, CampoResponsable);
        if (validacion.IsFail)
            return validacion;

        DebeCambiarPassword = true;
        RegistrarModificacion(usuarioModificacion, ahora);

        return Result.Ok();
    }

    private void RegistrarModificacion(string? usuarioModificacion, DateTime ahora)
    {
        UsuarioModificacion = Texto.NormalizarEspacios(usuarioModificacion);
        FechaModificacion = ahora;
    }

    private static Result ValidarDatos(string? nombres, string? apellidos, string? correo, int rolId) =>
        Validaciones.PrimerError(
            Validaciones.NoVacio(nombres, CampoNombres),
            Validaciones.SinCaracteresDeControl(nombres, CampoNombres),
            Validaciones.NoVacio(apellidos, CampoApellidos),
            Validaciones.SinCaracteresDeControl(apellidos, CampoApellidos),
            Validaciones.LongitudMaxima(
                ComponerNombreCompleto(nombres, apellidos),
                LongitudMaximaNombreCompleto,
                CampoNombres),
            Validaciones.NoVacio(correo, CampoCorreo),
            Validaciones.LongitudMaxima(correo, LongitudMaximaCorreo, CampoCorreo),
            Validaciones.Correo(correo, CampoCorreo),
            Validaciones.Positivo(rolId, CampoRol));

    private static Result ValidarUsername(string? username) =>
        Validaciones.PrimerError(
            Validaciones.NoVacio(username, CampoUsername),
            Validaciones.LongitudMaxima(username, LongitudMaximaUsername, CampoUsername));

    private static string ComponerNombreCompleto(string? nombres, string? apellidos) =>
        Texto.NormalizarEspacios($"{nombres} {apellidos}");

    private static string NormalizarCorreo(string? correo) =>
        Texto.NormalizarEspacios(correo).ToLowerInvariant();

    private static string EnPalabras(bool activo) => activo ? "activo" : "inactivo";
}
