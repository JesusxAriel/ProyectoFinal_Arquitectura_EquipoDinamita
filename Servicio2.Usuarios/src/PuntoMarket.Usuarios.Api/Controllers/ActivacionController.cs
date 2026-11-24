using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PuntoMarket.Usuarios.Api.Contracts;
using PuntoMarket.Usuarios.Api.Contracts.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ActivarCuenta;

namespace PuntoMarket.Usuarios.Api.Controllers;

[ApiController]
[Route("api/activacion")]
[AllowAnonymous]
public sealed class ActivacionController(ActivarCuentaCasoDeUso activarCuenta) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ActivarCuentaRespuesta>> Activar(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ActivarCuentaSolicitud? solicitud,
        CancellationToken cancellationToken)
    {
        if (solicitud is null)
            return BadRequest(new ErrorRespuesta(ActivarCuentaCasoDeUso.EnlaceInvalido));

        var resultado = await activarCuenta.EjecutarAsync(
            new ActivarCuentaEntrada(solicitud.Token, solicitud.NuevaContrasena, solicitud.ConfirmarContrasena),
            cancellationToken);

        if (resultado.IsOk)
            return Ok(new ActivarCuentaRespuesta(resultado.Value.Activada));

        return resultado.Error == ActivarCuentaCasoDeUso.ActivacionNoCompletada
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorRespuesta(resultado.Error))
            : BadRequest(new ErrorRespuesta(resultado.Error));
    }
}
