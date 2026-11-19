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

public sealed class SesionRequeridaMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, ISesionUsuario sesion)
    {
        var path = context.Request.Path;
        if (EsRutaPublica(path))
        {
            return next(context);
        }

        var datos = sesion.Actual;
        if (datos is null)
        {
            context.Response.Redirect(RutasAuth.Login);
            return Task.CompletedTask;
        }

        if (datos.DebeCambiarPassword)
        {
            context.Response.Redirect(RutasAuth.CambiarPasswordObligatorio);
            return Task.CompletedTask;
        }

        return next(context);
    }

    private static bool EsRutaPublica(PathString path) =>
        path.StartsWithSegments(RutasAuth.Prefijo) || path.StartsWithSegments(RutasAuth.Error);
}
