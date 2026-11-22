using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PuntoMarket.Usuarios.Api.Contracts;
using PuntoMarket.Usuarios.Api.Contracts.Gestion;
using PuntoMarket.Usuarios.Api.Filters;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CrearUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;

namespace PuntoMarket.Usuarios.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
[GestionExceptionFilter(StatusCodes.Status409Conflict)]
public sealed class UsuariosController(
    ListarUsuariosCasoDeUso listarUsuarios,
    CrearUsuarioCasoDeUso crearUsuario,
    IUsuarioGestionRepositorio repositorio) : ControllerBase
{
    private const string CuerpoRequerido = "Envíe los nombres, los apellidos, el correo y el rol.";

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

    [HttpPost]
    public async Task<ActionResult<CrearUsuarioRespuesta>> Crear(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateUsuarioRequest? solicitud,
        CancellationToken cancellationToken)
    {
        if (solicitud is null)
            return BadRequest(new ErrorRespuesta(CuerpoRequerido));

        var resultado = await crearUsuario.EjecutarAsync(
            new CrearUsuarioEntrada(
                solicitud.Nombres, solicitud.Apellidos, solicitud.Correo, solicitud.RolId),
            cancellationToken);

        if (resultado.IsFail)
            return BadRequest(new ErrorRespuesta(resultado.Error));

        var salida = resultado.Value;

        return StatusCode(
            StatusCodes.Status201Created,
            new CrearUsuarioRespuesta(salida.Id, salida.Username, salida.CorreoEnviado));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RolActivoRespuesta>>> ListarRoles(
        CancellationToken cancellationToken)
    {
        var roles = await repositorio.ListarRolesActivosAsync(cancellationToken);
        return Ok(roles.Select(rol => new RolActivoRespuesta(rol.Id, rol.Nombre)).ToArray());
    }
}
