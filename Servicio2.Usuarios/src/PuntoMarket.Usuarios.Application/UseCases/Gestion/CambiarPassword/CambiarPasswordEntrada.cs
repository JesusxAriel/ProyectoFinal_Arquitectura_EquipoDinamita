namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.CambiarPassword;

public sealed record CambiarPasswordEntrada(string? Actual, string? Nueva, string? Confirmacion);
