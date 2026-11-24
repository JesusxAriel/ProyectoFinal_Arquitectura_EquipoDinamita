namespace PuntoMarket.Usuarios.Api.Contracts.Gestion;

public sealed record CreateUsuarioRequest(string? Nombres, string? ApellidoPaterno, string? ApellidoMaterno, string? Correo, int RolId);
