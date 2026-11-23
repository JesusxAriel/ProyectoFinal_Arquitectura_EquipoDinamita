namespace PuntoMarket.Usuarios.Api.Contracts.Gestion;

public sealed record UsuarioResponse(
    int Id,
    string Username,
    string NombreCompleto,
    string Correo,
    string Rol,
    bool Activo,
    bool DebeCambiarPassword);

public sealed record RolActivoRespuesta(int Id, string Nombre);

public sealed record CrearUsuarioRespuesta(int Id, string Username, bool CorreoEnviado);

public sealed record ReenviarAccesoRespuesta(bool CorreoEnviado);
