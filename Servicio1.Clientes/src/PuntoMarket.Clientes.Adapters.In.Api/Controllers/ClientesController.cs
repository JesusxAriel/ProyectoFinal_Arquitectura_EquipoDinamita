using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Clientes.Adapters.In.Api.Http;
using PuntoMarket.Clientes.Adapters.In.Api.Security;
using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Application.Ports.In;

namespace PuntoMarket.Clientes.Adapters.In.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public sealed class ClientesController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Lectura)]
    [ProducesResponseType(typeof(PaginaResultado<ClienteDto>), 200)]
    public async Task<IActionResult> Listar([FromQuery] ListarClientesQuery query, [FromServices] IListarClientes caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(query, ct);
        return r.IsSuccess ? Ok(r.Value) : this.ToProblem(r.Error);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.Lectura)]
    public async Task<IActionResult> Obtener(int id, [FromServices] IObtenerCliente caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(id, ct);
        return r.IsSuccess ? Ok(r.Value) : this.ToProblem(r.Error);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Gestion)]
    public async Task<IActionResult> Crear([FromBody] GuardarClienteRequest request, [FromServices] ICrearCliente caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(request, ct);
        return r.IsSuccess
            ? CreatedAtAction(nameof(Obtener), new { id = r.Value.Id }, r.Value)
            : this.ToProblem(r.Error);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Gestion)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarClienteRequest request, [FromServices] IActualizarCliente caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(id, request, ct);
        return r.IsSuccess ? Ok(r.Value) : this.ToProblem(r.Error);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.Baja)]
    public async Task<IActionResult> Desactivar(int id, [FromServices] IDesactivarCliente caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(id, ct);
        return r.IsSuccess ? NoContent() : this.ToProblem(r.Error);
    }

    [HttpPatch("{id:int}/reactivar")]
    [Authorize(Policy = Policies.Baja)]
    public async Task<IActionResult> Reactivar(int id, [FromServices] IReactivarCliente caso, CancellationToken ct)
    {
        var r = await caso.EjecutarAsync(id, ct);
        return r.IsSuccess ? Ok(r.Value) : this.ToProblem(r.Error);
    }
}
