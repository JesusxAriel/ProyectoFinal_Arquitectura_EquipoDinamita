using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using PuntoMarket.Usuarios.Api.Contracts;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ConsultarCambioObligatorio;

namespace PuntoMarket.Usuarios.Api.Filters;

public sealed class CambioPasswordObligatorioFilter(ConsultarCambioObligatorioCasoDeUso consultarCambio)
    : IAsyncActionFilter
{
    public const string MensajeCambioObligatorio = "Debe cambiar su contraseña antes de continuar.";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!EstaAutenticado(context) || EstaExento(context))
        {
            await next();
            return;
        }

        var consulta = await consultarCambio.EjecutarAsync(
            new ConsultarCambioObligatorioEntrada(), context.HttpContext.RequestAborted);

        if (consulta.IsOk && consulta.Value)
        {
            context.Result = new ObjectResult(new ErrorRespuesta(MensajeCambioObligatorio))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }

    private static bool EstaAutenticado(ActionExecutingContext context) =>
        context.HttpContext.User.Identity?.IsAuthenticated == true;

    private static bool EstaExento(ActionExecutingContext context) =>
        context.ActionDescriptor is ControllerActionDescriptor accion
        && accion.MethodInfo.IsDefined(typeof(PermiteConCambioPasswordPendienteAttribute), inherit: false);
}
