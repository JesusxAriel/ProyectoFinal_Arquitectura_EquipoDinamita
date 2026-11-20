namespace PuntoMarket.Frontend.Catalogos;

// Presentación HTTP: conserva el contrato de errores de ambas APIs sin alterar Result de autenticación.
public sealed record ResultadoOperacion<T>(bool Ok, int Status, T? Data, string? Mensaje,
    IReadOnlyDictionary<string, string[]>? ErroresCampos = null, string? Codigo = null);
