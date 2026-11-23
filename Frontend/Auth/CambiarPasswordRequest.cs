namespace PuntoMarket.Frontend.Auth;

public record CambiarPasswordRequest(
    string Actual,
    string Nueva,
    string Confirmacion);
