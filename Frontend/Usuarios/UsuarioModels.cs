namespace PuntoMarket.Frontend.Usuarios;

public sealed record CreateUsuarioRequest(string Nombres, string Apellidos, string Correo, int RolId);

public sealed record UpdateUsuarioRequest(string Nombres, string Apellidos, string Correo, int RolId);

public sealed record ChangeStatusRequest(bool Activo);

public sealed record UsuarioResponse(
    int Id,
    string Username,
    string NombreCompleto,
    string Correo,
    string Rol,
    bool Activo,
    bool DebeCambiarPassword);

public sealed record CreateUsuarioResponse(
    int Id,
    string Username,
    bool CorreoEnviado);

public sealed record ReenviarAccesoResponse(bool CorreoEnviado);

public sealed record RolResponse(int Id, string Nombre);