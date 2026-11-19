using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using PuntoMarket.Usuarios.Application.Ports.Comunes;

namespace PuntoMarket.Usuarios.Api.Security;

public static class AutenticacionKeycloakExtensions
{
    public static IServiceCollection AgregarAutenticacionKeycloak(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var opciones = configuracion.GetSection(JwtOpciones.Seccion).Get<JwtOpciones>() ?? new JwtOpciones();

        servicios
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = opciones.Authority;
                jwt.Audience = opciones.Audience;
                jwt.RequireHttpsMetadata = opciones.RequireHttpsMetadata;
                jwt.MapInboundClaims = false;
                jwt.Events = new JwtBearerEvents
                {
                    OnTokenValidated = contexto =>
                    {
                        if (contexto.Principal?.Identity is ClaimsIdentity identidad)
                        {
                            KeycloakRolesMapper.AgregarRoles(identidad);
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        servicios.AddHttpContextAccessor();
        servicios.AddScoped<IUsuarioActual, UsuarioActualHttp>();
        return servicios;
    }
}
