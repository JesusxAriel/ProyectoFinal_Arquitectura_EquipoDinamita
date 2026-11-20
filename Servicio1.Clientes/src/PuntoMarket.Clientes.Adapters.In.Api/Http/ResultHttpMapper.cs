using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Clientes.Domain.Common;

namespace PuntoMarket.Clientes.Adapters.In.Api.Http;

public static class ResultHttpMapper
{
    public static IActionResult ToProblem(this ControllerBase _, Error error)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (400, "Datos inválidos"),
            ErrorType.NotFound => (404, "Recurso no encontrado"),
            ErrorType.Conflict => (409, "Conflicto de negocio"),
            ErrorType.Unauthorized => (401, "No autenticado"),
            ErrorType.Unavailable => (503, "Servicio no disponible"),
            _ => (500, "Error interno")
        };
        var problem = new ProblemDetails { Status = status, Title = title, Detail = error.Message };
        problem.Extensions["code"] = error.Code;
        if (error.Fields is not null) problem.Extensions["errors"] = error.Fields;
        return new ObjectResult(problem) { StatusCode = status };
    }
}
