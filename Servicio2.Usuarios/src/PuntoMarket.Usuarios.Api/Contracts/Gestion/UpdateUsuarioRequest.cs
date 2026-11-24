namespace PuntoMarket.Usuarios.Api.Contracts.Gestion;

public sealed record UpdateUsuarioRequest(string? Nombres, string? ApellidoPaterno, string? ApellidoMaterno, string? Correo, int RolId);
