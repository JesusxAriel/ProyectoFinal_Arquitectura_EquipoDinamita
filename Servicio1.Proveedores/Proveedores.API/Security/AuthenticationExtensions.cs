using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Proveedores.API.Security;

public static class AuthenticationExtensions
{
    /// <summary>Registra JwtBearer (validación) + políticas de autorización por rol. Ver AuthSettings.cs para la configuración.</summary>
    public static IServiceCollection AddProveedoresAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthSettings>(configuration.GetSection(AuthSettings.SectionName));
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddAuthorization(o =>
        {
            // Administrador y Cajero pueden listar y buscar.
            o.AddPolicy(Policies.Lectura, p => p.RequireAuthenticatedUser().RequireRole(Roles.Administrador, Roles.Cajero));
            // Solo Administrador puede crear, modificar, desactivar y reactivar.
            o.AddPolicy(Policies.Escritura, p => p.RequireAuthenticatedUser().RequireRole(Roles.Administrador));
        });

        services.AddHttpContextAccessor();
        services.AddScoped<Proveedores.Application.Abstractions.ICurrentUser, HttpContextCurrentUser>();
        return services;
    }
}
