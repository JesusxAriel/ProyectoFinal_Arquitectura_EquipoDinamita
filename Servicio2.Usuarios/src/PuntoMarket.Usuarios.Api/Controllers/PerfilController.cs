using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PuntoMarket.Usuarios.Api.Contracts;
using PuntoMarket.Usuarios.Api.Contracts.Perfil;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CambiarPassword;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ObtenerPerfil;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.VerificarPasswordActual;

namespace PuntoMarket.Usuarios.Api.Controllers;

[ApiController]
[Route("api/usuarios/perfil")]
[Authorize]
public sealed class PerfilController(
    ObtenerPerfilCasoDeUso obtenerPerfil,
    CambiarPasswordCasoDeUso cambiarPassword,
    VerificarPasswordActualCasoDeUso verificarPasswordActual) : ControllerBase
{
    private const string CuerpoRequerido = "Envíe la contraseña actual, la nueva y su confirmación.";

    [HttpGet]
    public async Task<ActionResult<PerfilRespuesta>> Obtener(CancellationToken cancellationToken)
    {
        var resultado = await obtenerPerfil.EjecutarAsync(new ObtenerPerfilEntrada(), cancellationToken);
        if (resultado.IsFail)
            return NotFound(new ErrorRespuesta(resultado.Error));

        var perfil = resultado.Value;

        return Ok(new PerfilRespuesta(
            perfil.Username,
            perfil.NombreCompleto,
            perfil.Rol,
            perfil.Activo,
            perfil.DebeCambiarPassword));
    }

    [HttpPost("password/verificacion")]
    public async Task<ActionResult<VerificarPasswordActualRespuesta>> VerificarPasswordActual(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] VerificarPasswordActualRequest? solicitud)
    {
        var resultado = await verificarPasswordActual.EjecutarAsync(
            new VerificarPasswordActualEntrada(solicitud?.Actual));

        return resultado.IsFail
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorRespuesta(resultado.Error))
            : Ok(new VerificarPasswordActualRespuesta(resultado.Value.Valida));
    }

    [HttpPut("password")]
    public async Task<IActionResult> CambiarPassword(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CambiarPasswordRequest? solicitud,
        CancellationToken cancellationToken)
    {
        if (solicitud is null)
            return BadRequest(new ErrorRespuesta(CuerpoRequerido));

        var resultado = await cambiarPassword.EjecutarAsync(
            new CambiarPasswordEntrada(solicitud.Actual, solicitud.Nueva, solicitud.Confirmacion),
            cancellationToken);

        return resultado.IsFail
            ? BadRequest(new ErrorRespuesta(resultado.Error))
            : NoContent();
    }
}
