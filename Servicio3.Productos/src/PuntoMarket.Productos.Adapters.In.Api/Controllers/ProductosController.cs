using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Productos.Application;
using PuntoMarket.Productos.Application.Ports.In;
using PuntoMarket.Productos.Domain;
using PuntoMarket.Productos.Adapters.In.Api.Http;
using PuntoMarket.Productos.Adapters.In.Api.Security;
namespace PuntoMarket.Productos.Adapters.In.Api.Controllers;

[ApiController, Route("api/productos"), Authorize(Policy=Policies.Lectura)]
public sealed class ProductosController(IListarProductos listar,IObtenerProducto obtener,ICrearProducto crear,
    IActualizarProducto actualizar,IDesactivarProducto desactivar,IConsultarCatalogos catalogos) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] ListarProductosQuery query,CancellationToken ct) {
        var r=await listar.EjecutarAsync(query,ct); return r.IsSuccess?Ok(r.Value):this.ToProblem(r.Error);
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id,CancellationToken ct) {
        var r=await obtener.EjecutarAsync(id,ct); return r.IsSuccess?Ok(r.Value):this.ToProblem(r.Error);
    }
    [HttpGet("catalogos")]
    public async Task<IActionResult> Catalogos(CancellationToken ct) {
        var r=await catalogos.EjecutarAsync(ct); return r.IsSuccess?Ok(r.Value):this.ToProblem(r.Error);
    }
    [HttpPost,Authorize(Policy=Policies.Gestion)]
    public async Task<IActionResult> Crear(DatosProducto datos,CancellationToken ct) {
        var r=await crear.EjecutarAsync(datos,ct); return r.IsSuccess?CreatedAtAction(nameof(Obtener),new{id=r.Value.Id},r.Value):this.ToProblem(r.Error);
    }
    [HttpPut("{id:int}"),Authorize(Policy=Policies.Gestion)]
    public async Task<IActionResult> Actualizar(int id,DatosProducto datos,CancellationToken ct) {
        var r=await actualizar.EjecutarAsync(id,datos,ct); return r.IsSuccess?Ok(r.Value):this.ToProblem(r.Error);
    }
    [HttpDelete("{id:int}"),Authorize(Policy=Policies.Baja)]
    public async Task<IActionResult> Desactivar(int id,CancellationToken ct) {
        var r=await desactivar.EjecutarAsync(id,ct); return r.IsSuccess?NoContent():this.ToProblem(r.Error);
    }
}
