namespace Proveedores.Infrastructure.Integrations;

// =====================================================
// INTEGRACIÓN FUTURA - API DE PRODUCTOS (configuración externa)
// ESTADO: PREPARADO. Sección "ProductosApi" / variables ProductosApi__BaseUrl, ProductosApi__Enabled.
// Mientras Enabled=false (valor por defecto) el adaptador devuelve Failure "no configurada".
// =====================================================
public sealed class ProductosApiSettings
{
    public const string SectionName = "ProductosApi";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 5;

    /// <summary>
    /// CONTRATO PROPUESTO (a acordar con el responsable de Productos). {proveedorId} se sustituye en la ruta.
    /// Respuesta esperada 200: { "cantidad": 3 }
    /// </summary>
    public string RutaConteoActivos { get; set; } = "api/productos/proveedor/{proveedorId}/activos/conteo";
}
