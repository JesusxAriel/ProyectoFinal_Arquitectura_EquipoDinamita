using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace PuntoMarket.Gateway.Security;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = settings.Authority;
                options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidateAudience = settings.ValidateAudience;
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity)
                        {
                            KeycloakRealmRolesMapper.AddRoleClaims(identity);
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(GatewayPolicies.TokenValido, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(GatewayPolicies.SoloAdministrador, policy => policy.RequireRole(GatewayRoles.Administrador));

        return services;
    }
}
