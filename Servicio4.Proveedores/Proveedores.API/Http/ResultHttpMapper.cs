using Microsoft.AspNetCore.Mvc;
using Proveedores.Domain.Common;

namespace Proveedores.API.Http;

/// <summary>
/// Traduce un Result a una respuesta HTTP (ProblemDetails con "code").
/// Validation=400, NotFound=404, Conflict=409, Unauthorized=401, Unavailable=503.
/// El 200/201/204 de éxito lo decide cada acción del controlador.
/// </summary>
public static class ResultHttpMapper
{
    public static IActionResult ToProblem(this ControllerBase controlador, Error error)
    {
        var (estado, titulo) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Datos inválidos"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflicto de negocio"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "No autenticado"),
            ErrorType.Unavailable => (StatusCodes.Status503ServiceUnavailable, "Servicio no disponible"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno")
        };

        var problema = new ProblemDetails { Status = estado, Title = titulo, Detail = error.Message };
        problema.Extensions["code"] = error.Code;
        if (error.Fields is not null) problema.Extensions["errors"] = error.Fields;
        return new ObjectResult(problema) { StatusCode = estado };
    }
}
