using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Clientes.Adapters.In.Api.Http;
using PuntoMarket.Clientes.Adapters.In.Api.Security;
using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Application.Ports.In;

namespace PuntoMarket.Clientes.Adapters.In.Api.Controllers;

/// <summary>
/// ADAPTADOR DE ENTRADA HTTP del microservicio Clientes. Controlador delgado: delega en los puertos de
/// entrada (casos de uso) y traduce el Result a HTTP. Los permisos se comprueban AQUÍ (servidor):
/// lectura y gestión = Administrador o Cajero; baja y reactivación = solo Administrador.
/// </summary>
[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public sealed class ClientesController : ControllerBase
{
    /// <summary>Lista clientes con búsqueda, filtro de estado, ordenación y paginación.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.Lectura)]
    [ProducesResponseType(typeof(PaginaResultado<ClienteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] ListarClientesQuery consulta, [FromServices] IListarClientes caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(consulta, ct);
        return r.IsSuccess ? Ok(r.Value) : this.ToProblem(r.Error);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.Lectura)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(int id, [FromServices] IObtenerCliente caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(id, ct);
        return r.IsSuccess ? Ok(r.Value) : this.ToProblem(r.Error);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Gestion)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(
        [FromBody] GuardarClienteRequest request, [FromServices] ICrearCliente caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(request, ct);
        return r.IsSuccess
            ? CreatedAtAction(nameof(Obtener), new { id = r.Value.Id }, r.Value)
            : this.ToProblem(r.Error);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Gestion)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] GuardarClienteRequest request, [FromServices] IActualizarCliente caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(id, request, ct);
        return r.IsSuccess ? Ok(r.Value) : this.ToProblem(r.Error);
    }

    /// <summary>Baja lógica: estaActivo = false (no se borra la fila).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.Baja)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Desactivar(int id, [FromServices] IDesactivarCliente caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(id, ct);
        return r.IsSuccess ? NoContent() : this.ToProblem(r.Error);
    }

    [HttpPatch("{id:int}/reactivar")]
    [Authorize(Policy = Policies.Baja)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reactivar(int id, [FromServices] IReactivarCliente caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(id, ct);
        return r.IsSuccess ? Ok(r.Value) : this.ToProblem(r.Error);
    }
}
