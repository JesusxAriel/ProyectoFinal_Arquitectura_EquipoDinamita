namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ObtenerPerfil;

public sealed record ObtenerPerfilSalida(
    string Username,
    string NombreCompleto,
    string Rol,
    bool Activo,
    bool DebeCambiarPassword);
