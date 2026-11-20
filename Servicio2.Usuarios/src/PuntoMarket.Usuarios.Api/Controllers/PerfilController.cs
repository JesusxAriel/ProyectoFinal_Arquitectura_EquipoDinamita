using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Usuarios.Api.Contracts;
using PuntoMarket.Usuarios.Api.Contracts.Perfil;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ObtenerPerfil;

namespace PuntoMarket.Usuarios.Api.Controllers;

[ApiController]
[Route("api/usuarios/perfil")]
[Authorize]
public sealed class PerfilController(ObtenerPerfilCasoDeUso obtenerPerfil) : ControllerBase
{
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
}
