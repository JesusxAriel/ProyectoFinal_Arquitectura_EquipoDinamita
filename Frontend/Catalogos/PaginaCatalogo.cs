using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Catalogos;

// Exclusivo de los dos nuevos módulos: no modifica el middleware ni crea otra sesión.
public abstract class PaginaCatalogo : PageModel
{
    public bool EsAdministrador => User.IsInRole("Administrador");
    protected bool PuedeConsultar => User.IsInRole("Administrador") || User.IsInRole("Cajero");
    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (User.Identity?.IsAuthenticated != true) { context.Result = Redirect(RutasAuth.Login); return; }
        if (!PuedeConsultar) { context.Result = StatusCode(403); return; }
        await next();
    }
    protected string? MensajeApi<T>(ResultadoOperacion<T> resultado)
    {
        // 401/403 se presentan controladamente; no se renueva ni se modifica la sesión compartida.
        if (resultado.Status is 401 or 403) Response.StatusCode = resultado.Status;
        return resultado.Mensaje;
    }
    protected void LimpiarValidacionFormulario()
    {
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Form.")).ToArray()) ModelState.Remove(key);
    }
}
