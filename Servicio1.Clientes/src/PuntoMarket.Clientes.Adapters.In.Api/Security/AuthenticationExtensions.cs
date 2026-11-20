using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using PuntoMarket.Clientes.Application.Ports.Out;

namespace PuntoMarket.Clientes.Adapters.In.Api.Security;

public static class AuthenticationExtensions
{
    /// <summary>Registra JwtBearer (validación) + políticas de autorización por rol. Ver AuthSettings.cs para la configuración.</summary>
    public static IServiceCollection AddClientesAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthSettings>(configuration.GetSection(AuthSettings.SectionName));
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddAuthorization(o =>
        {
            o.AddPolicy(Policies.Lectura, p => p.RequireAuthenticatedUser().RequireRole(Roles.Administrador, Roles.Cajero));
            o.AddPolicy(Policies.Gestion, p => p.RequireAuthenticatedUser().RequireRole(Roles.Administrador, Roles.Cajero));
            o.AddPolicy(Policies.Baja, p => p.RequireAuthenticatedUser().RequireRole(Roles.Administrador));
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        return services;
    }
}
