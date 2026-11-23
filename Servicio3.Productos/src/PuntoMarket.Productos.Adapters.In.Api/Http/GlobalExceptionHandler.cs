using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
namespace PuntoMarket.Productos.Adapters.In.Api.Http;
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context,Exception exception,CancellationToken ct)
    {
        logger.LogError(exception,"Error técnico en Productos.");
        var status=exception is NpgsqlException or TimeoutException?503:500;
        context.Response.StatusCode=status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails {
            Status=status,Title="Servicio no disponible",Detail="No se pudo completar la operación. Intente nuevamente.",
            Extensions={["code"]=status==503?"Producto.PersistenciaNoDisponible":"Producto.ErrorInterno"}
        },ct); return true;
    }
}
