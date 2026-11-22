namespace PuntoMarket.Usuarios.Api.Contracts.Gestion;

public sealed record CreateUsuarioRequest(string? Nombres, string? Apellidos, string? Correo, int RolId);
