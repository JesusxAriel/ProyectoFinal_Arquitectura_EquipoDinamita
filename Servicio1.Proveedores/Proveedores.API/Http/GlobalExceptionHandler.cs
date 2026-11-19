using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Proveedores.API.Http;

/// <summary>
/// Manejo centralizado de errores TÉCNICOS inesperados (no de validación de negocio, que usa Result).
/// No expone detalles internos al cliente; el detalle queda en el log.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error técnico no controlado en {Metodo} {Ruta}.", httpContext.Request.Method, httpContext.Request.Path);

        var (estado, titulo) = exception is MongoException or TimeoutException
            ? (StatusCodes.Status503ServiceUnavailable, "Persistencia no disponible")
            : (StatusCodes.Status500InternalServerError, "Error interno del servidor");

        httpContext.Response.StatusCode = estado;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = estado, Title = titulo, Detail = "Ocurrió un error inesperado. Intente nuevamente." },
            cancellationToken);
        return true;
    }
}
