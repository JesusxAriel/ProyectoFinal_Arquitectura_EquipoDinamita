using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Frontend.Demo.Services;

// DEMO TEMPORAL: solo protege la navegación; los JWT y permisos se validan en la API.
// INTEGRACIÓN: También se aplicará a nuevas Razor Pages. No deducir permisos de Usuarios de este
//   filtro ni de la política de Proveedores; acordar la matriz de roles con el dueño de cada API.
public sealed class DemoPerfilRequeridoFilter(DemoPerfilSession perfil) : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (context.ActionDescriptor.ViewEnginePath is not ("/Login" or "/Error") && !perfil.TienePerfil)
        {
            context.Result = new RedirectToPageResult("/Login");
            return;
        }
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        await next();
    }
}
