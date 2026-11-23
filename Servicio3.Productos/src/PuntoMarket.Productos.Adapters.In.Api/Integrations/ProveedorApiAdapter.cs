using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PuntoMarket.Productos.Application.Ports.Out;
using PuntoMarket.Productos.Domain;
namespace PuntoMarket.Productos.Adapters.In.Api.Integrations;

// Adaptador HTTP de salida ubicado en el host para propagar el Bearer de la solicitud.
// No referencia MongoDB ni las bibliotecas del otro servicio.
public sealed class ProveedorApiAdapter(HttpClient http, IHttpContextAccessor contexto) : IProveedorApiClient
{
    public async Task<Result> ValidarActivoAsync(string id,CancellationToken ct)
    {
        using var req=new HttpRequestMessage(HttpMethod.Get,"api/proveedores/"+Uri.EscapeDataString(id));
        var bearer=contexto.HttpContext?.Request.Headers.Authorization.ToString();
        if(string.IsNullOrWhiteSpace(bearer)) return Result.Failure(Error.Unavailable("Proveedor.AutenticacionNoDisponible","No se pudo autenticar la consulta de Proveedores."));
        req.Headers.TryAddWithoutValidation("Authorization",bearer);
        try {
            using var response=await http.SendAsync(req,ct);
            if(response.StatusCode==HttpStatusCode.NotFound) return Result.Failure(Error.Validation("Producto.ProveedorNoEncontrado","El proveedor seleccionado no existe."));
            if(!response.IsSuccessStatusCode) return NoDisponible();
            var proveedor=await response.Content.ReadFromJsonAsync<ProveedorRespuesta>(ct);
            if(proveedor is null || proveedor.Id!=id || proveedor.EstaActivo is null) return NoDisponible();
            return proveedor.EstaActivo.Value ? Result.Success() : Result.Failure(Error.Validation("Producto.ProveedorInactivo","El proveedor seleccionado está desactivado."));
        } catch(Exception e) when(e is HttpRequestException or JsonException or NotSupportedException || e is OperationCanceledException && !ct.IsCancellationRequested) { return NoDisponible(); }
    }
    private static Result NoDisponible() => Result.Failure(Error.Unavailable("Proveedor.NoDisponible","No se pudo verificar el proveedor. Intente nuevamente; no se guardaron los cambios."));
    private sealed record ProveedorRespuesta(string? Id,bool? EstaActivo);
}
