namespace PuntoMarket.Usuarios.Api.Contracts.Perfil;

public sealed record PerfilRespuesta(
    string Username,
    string NombreCompleto,
    string Rol,
    bool Activo,
    bool DebeCambiarPassword);
