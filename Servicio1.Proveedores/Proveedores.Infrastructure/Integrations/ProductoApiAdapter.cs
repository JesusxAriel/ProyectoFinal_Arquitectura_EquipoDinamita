using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proveedores.Application.Abstractions;
using Proveedores.Domain.Common;

namespace Proveedores.Infrastructure.Integrations;

// =====================================================
// PATRÓN DE DISEÑO 2/4: ADAPTER (integración futura con Productos)
// ESTADO: PREPARADO. NO se invoca desde los casos de uso CRUD de esta entrega.
//
// Adapta el contrato HTTP del (futuro) microservicio de Productos al puerto
// IProductoApiClient de Application. No simula respuestas: si no está configurado
// o la API falla, devuelve Result.Failure (nunca "false" inventado).
//
// QUÉ DEBE HACER QUIEN INTEGRE: activar ProductosApi__Enabled=true y ProductosApi__BaseUrl,
// confirmar la ruta/forma de la respuesta y llamar al puerto desde DesactivarProveedor.
// Se prueba sin API real en Proveedores.Tests/ProductoApiAdapterTests.cs con un HttpMessageHandler falso.
// =====================================================
public sealed class ProductoApiAdapter(
    HttpClient http,
    IOptions<ProductosApiSettings> opciones,
    ILogger<ProductoApiAdapter> logger) : IProductoApiClient
{
    public static readonly Error NoConfigurada = Error.Unavailable(
        "Integracion.Productos.NoConfigurada", "La integración con la API de Productos no está configurada.");
    public static readonly Error NoDisponible = Error.Unavailable(
        "Integracion.Productos.NoDisponible", "La API de Productos no está disponible.");
    public static readonly Error RespuestaInvalida = Error.Unavailable(
        "Integracion.Productos.RespuestaInvalida", "La API de Productos devolvió una respuesta inesperada.");

    public async Task<Result<bool>> TieneProductosActivosAsync(string proveedorId, CancellationToken cancellationToken)
    {
        var settings = opciones.Value;
        if (!settings.Enabled || http.BaseAddress is null)
            return Result.Failure<bool>(NoConfigurada);

        var ruta = settings.RutaConteoActivos.Replace("{proveedorId}", Uri.EscapeDataString(proveedorId));

        try
        {
            using var respuesta = await http.GetAsync(ruta, cancellationToken);
            if (respuesta.StatusCode != HttpStatusCode.OK)
            {
                logger.LogWarning("API de Productos respondió {Status} al consultar el proveedor {Id}.", (int)respuesta.StatusCode, proveedorId);
                return Result.Failure<bool>(NoDisponible);
            }

            var cuerpo = await respuesta.Content.ReadFromJsonAsync<ConteoProductosRespuesta>(cancellationToken);
            return cuerpo is null || cuerpo.Cantidad < 0
                ? Result.Failure<bool>(RespuestaInvalida)
                : Result.Success(cuerpo.Cantidad > 0);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "No se pudo contactar con la API de Productos.");
            return Result.Failure<bool>(NoDisponible);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Tiempo de espera agotado al contactar con la API de Productos.");
            return Result.Failure<bool>(NoDisponible);
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "Respuesta no válida de la API de Productos.");
            return Result.Failure<bool>(RespuestaInvalida);
        }
    }

    private sealed record ConteoProductosRespuesta(
        [property: JsonRequired, JsonPropertyName("cantidad"), JsonNumberHandling(JsonNumberHandling.Strict)] int Cantidad);
}
