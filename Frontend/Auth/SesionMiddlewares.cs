using System.Security.Claims;

namespace PuntoMarket.Frontend.Auth;

public sealed class SesionAutenticacionMiddleware(RequestDelegate next)
{
    private const string TipoAutenticacion = "Sesion";

    public Task InvokeAsync(HttpContext context, ISesionUsuario sesion)
    {
        var datos = sesion.Actual;
        if (datos is not null)
        {
            context.User = BuildPrincipal(datos);
        }

        return next(context);
    }

    private static ClaimsPrincipal BuildPrincipal(DatosSesion datos)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, datos.Username),
            new Claim(ClaimTypes.Name, datos.NombreCompleto),
            new Claim(ClaimTypes.Role, datos.Rol)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, TipoAutenticacion));
    }
}

public sealed class CambioPasswordObligatorioMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, ISesionUsuario sesion)
    {
        var debeCambiar = sesion.Actual?.DebeCambiarPassword == true;
        if (debeCambiar && !context.Request.Path.StartsWithSegments(RutasAuth.Prefijo))
        {
            context.Response.Redirect(RutasAuth.CambiarPasswordObligatorio);
            return Task.CompletedTask;
        }

        return next(context);
    }
}
