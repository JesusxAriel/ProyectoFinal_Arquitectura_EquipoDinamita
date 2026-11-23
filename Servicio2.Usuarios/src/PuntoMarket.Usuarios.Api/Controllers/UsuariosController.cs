using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PuntoMarket.Usuarios.Api.Contracts;
using PuntoMarket.Usuarios.Api.Contracts.Gestion;
using PuntoMarket.Usuarios.Api.Filters;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ActualizarUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CambiarEstadoUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CrearUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ObtenerUsuario;

namespace PuntoMarket.Usuarios.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
[GestionExceptionFilter(StatusCodes.Status409Conflict)]
public sealed class UsuariosController(
    ListarUsuariosCasoDeUso listarUsuarios,
    ObtenerUsuarioCasoDeUso obtenerUsuario,
    CrearUsuarioCasoDeUso crearUsuario,
    ActualizarUsuarioCasoDeUso actualizarUsuario,
    CambiarEstadoUsuarioCasoDeUso cambiarEstadoUsuario,
    IUsuarioRepositorio repositorio) : ControllerBase
{
    private const string CuerpoRequerido = "Envíe los nombres, los apellidos, el correo y el rol.";
    private const string EstadoRequerido = "Indique si el usuario queda activo.";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsuarioResponse>>> Listar(
        CancellationToken cancellationToken)
    {
        var resultado = await listarUsuarios.EjecutarAsync(new ListarUsuariosEntrada(), cancellationToken);
        if (resultado.IsFail)
            return Problema(resultado.Error);

        return Ok(resultado.Value.Usuarios.Select(Mapear).ToArray());
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RolActivoRespuesta>>> ListarRoles(
        CancellationToken cancellationToken)
    {
        var roles = await repositorio.ListarRolesActivos(cancellationToken);

        return Ok(roles.Select(rol => new RolActivoRespuesta(rol.Id, rol.Nombre)).ToArray());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioResponse>> Obtener(int id, CancellationToken cancellationToken)
    {
        var resultado = await obtenerUsuario.EjecutarAsync(new ObtenerUsuarioEntrada(id), cancellationToken);

        return resultado.IsFail
            ? Problema(resultado.Error)
            : Ok(Mapear(resultado.Value.Usuario));
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
            return Problema(resultado.Error);

        var salida = resultado.Value;

        return StatusCode(
            StatusCodes.Status201Created,
            new CrearUsuarioRespuesta(salida.Id, salida.Username, salida.CorreoEnviado));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioResponse>> Actualizar(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] UpdateUsuarioRequest? solicitud,
        CancellationToken cancellationToken)
    {
        if (solicitud is null)
            return BadRequest(new ErrorRespuesta(CuerpoRequerido));

        var resultado = await actualizarUsuario.EjecutarAsync(
            new ActualizarUsuarioEntrada(
                id, solicitud.Nombres, solicitud.Apellidos, solicitud.Correo, solicitud.RolId),
            cancellationToken);

        return resultado.IsFail
            ? Problema(resultado.Error)
            : Ok(Mapear(resultado.Value.Usuario));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ChangeStatusRequest? solicitud,
        CancellationToken cancellationToken)
    {
        if (solicitud is null)
            return BadRequest(new ErrorRespuesta(EstadoRequerido));

        var resultado = await cambiarEstadoUsuario.EjecutarAsync(
            new CambiarEstadoUsuarioEntrada(id, solicitud.Activo), cancellationToken);

        return resultado.IsFail ? Problema(resultado.Error) : NoContent();
    }

    private ActionResult Problema(string mensaje) =>
        mensaje == MensajesDeGestion.UsuarioNoEncontrado
            ? NotFound(new ErrorRespuesta(mensaje))
            : BadRequest(new ErrorRespuesta(mensaje));

    private static UsuarioResponse Mapear(UsuarioSalida usuario) =>
        new(usuario.Id,
            usuario.Username,
            usuario.NombreCompleto,
            usuario.Correo,
            usuario.Rol,
            usuario.Activo,
            usuario.DebeCambiarPassword);
}
