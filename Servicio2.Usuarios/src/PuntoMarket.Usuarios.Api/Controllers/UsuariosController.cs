using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Usuarios.Api.Contracts.Gestion;
using PuntoMarket.Usuarios.Api.Filters;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;

namespace PuntoMarket.Usuarios.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
[GestionExceptionFilter(StatusCodes.Status409Conflict)]
public sealed class UsuariosController(
    ListarUsuariosCasoDeUso listarUsuarios,
    IUsuarioGestionRepositorio repositorio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ListarUsuariosRespuesta>> Listar(CancellationToken cancellationToken)
    {
        var salida = await listarUsuarios.EjecutarAsync(new ListarUsuariosEntrada(), cancellationToken);
        var usuarios = salida.Usuarios.Select(usuario => new UsuarioListadoRespuesta(
            usuario.Id, usuario.Nombre, usuario.Apellido, usuario.Correo, usuario.Username,
            usuario.Rol, usuario.Estado, usuario.FechaCreacion, usuario.TokenActivacionExpira,
            usuario.FechaActivacion, usuario.EstaExpirado)).ToArray();

        return Ok(new ListarUsuariosRespuesta(usuarios));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RolActivoRespuesta>>> ListarRoles(
        CancellationToken cancellationToken)
    {
        var roles = await repositorio.ListarRolesActivosAsync(cancellationToken);
        return Ok(roles.Select(rol => new RolActivoRespuesta(rol.Id, rol.Nombre)).ToArray());
    }
}
