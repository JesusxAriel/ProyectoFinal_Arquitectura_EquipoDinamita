namespace PuntoMarket.Clientes.Domain.Common;

/// <summary>
/// Categoría de un error controlado. La capa API la traduce a un código HTTP
/// (ver Adapters.In.Api/Http/ResultHttpMapper.cs). El dominio NO conoce HTTP.
/// </summary>
public enum ErrorType
{
    None = 0,
    Validation = 1,   // -> 400
    NotFound = 2,     // -> 404
    Conflict = 3,     // -> 409
    Unauthorized = 4, // -> 401
    Unavailable = 5   // -> 503 (dependencia externa no disponible o no configurada)
}
