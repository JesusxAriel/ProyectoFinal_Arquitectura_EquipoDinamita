using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proveedores.API.Http;
using Proveedores.API.Security;
using Proveedores.Application.Dtos;
using Proveedores.Application.UseCases;

namespace Proveedores.API.Controllers;

/// <summary>
/// CRUD de Proveedores. Controlador delgado: delega en los casos de uso de Application y
/// traduce el Result a HTTP. Los permisos se comprueban AQUÍ (servidor), no en el frontend:
/// lectura = Administrador o Cajero; escritura = solo Administrador.
/// </summary>
[ApiController]
[Route("api/proveedores")]
[Produces("application/json")]
// INTEGRACIÓN: La API delega en Application; no escribe BSON ni consulta la base de otro servicio.
//   ResultHttpMapper traduce los errores esperados; los JWT y roles se validan antes del caso de uso.
public sealed class ProveedoresController : ControllerBase
{
    /// <summary>Lista proveedores con búsqueda, filtro de estado, orden y paginación.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.Lectura)]
    [ProducesResponseType(typeof(PaginaResultado<ProveedorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] ListarProveedoresQuery consulta,
        [FromServices] IListarProveedores caso,
        CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(consulta, ct);
        return r.IsSuccess ? Ok(r.Value) : this.ToProblem(r.Error);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Policies.Lectura)]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(string id, [FromServices] IObtenerProveedor caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(id, ct);
        return r.IsSuccess ? Ok(r.Value) : this.ToProblem(r.Error);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Escritura)]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(
        [FromBody] GuardarProveedorRequest request, [FromServices] ICrearProveedor caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(request, ct);
        return r.IsSuccess
            ? CreatedAtAction(nameof(Obtener), new { id = r.Value.Id }, r.Value)
            : this.ToProblem(r.Error);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.Escritura)]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(
        string id, [FromBody] GuardarProveedorRequest request, [FromServices] IActualizarProveedor caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(id, request, ct);
        return r.IsSuccess ? Ok(r.Value) : this.ToProblem(r.Error);
    }

    /// <summary>Eliminación lógica: estaActivo = false (no se borra el documento).</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.Escritura)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Desactivar(string id, [FromServices] IDesactivarProveedor caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(id, ct);
        return r.IsSuccess ? NoContent() : this.ToProblem(r.Error);
    }

    [HttpPatch("{id}/reactivar")]
    [Authorize(Policy = Policies.Escritura)]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reactivar(string id, [FromServices] IReactivarProveedor caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(id, ct);
        return r.IsSuccess ? Ok(r.Value) : this.ToProblem(r.Error);
    }
}
