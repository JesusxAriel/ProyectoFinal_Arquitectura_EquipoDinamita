using Proveedores.Domain.Common;

namespace Proveedores.Application.Abstractions;

// =====================================================
// INTEGRACIÓN FUTURA - API DE PRODUCTOS
// ESTADO: PREPARADO, NO INVOCADO desde los casos de uso CRUD.
//
// QUÉ HACE: define el contrato para preguntar al microservicio de Productos si un
//           proveedor tiene productos activos (regla original del SQL: no se puede
//           desactivar un proveedor con productos activos).
// QUÉ FALTA: acordar el contrato HTTP real con el dueño de Productos y llamar a este
//           puerto desde DesactivarProveedor (ver INTEGRACION_GRUPO2.md, sección 19).
// IMPORTANTE: devuelve Result<bool>. Si la API no está disponible o no está configurada
//           devuelve Failure; NUNCA "false" simulado.
// =====================================================
public interface IProductoApiClient
{
    Task<Result<bool>> TieneProductosActivosAsync(string proveedorId, CancellationToken cancellationToken);
}
