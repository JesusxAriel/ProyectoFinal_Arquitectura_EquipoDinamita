namespace PuntoMarket.Usuarios.Api.Contracts.Gestion;

public sealed record UpdateUsuarioRequest(string? Nombres, string? Apellidos, string? Correo, int RolId);
