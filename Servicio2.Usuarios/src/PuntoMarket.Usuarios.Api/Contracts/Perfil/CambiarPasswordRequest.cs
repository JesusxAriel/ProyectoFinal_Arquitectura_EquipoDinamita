namespace PuntoMarket.Usuarios.Api.Contracts.Perfil;

public sealed record CambiarPasswordRequest(string? Actual, string? Nueva, string? Confirmacion);
