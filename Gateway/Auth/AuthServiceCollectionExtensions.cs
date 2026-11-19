using Microsoft.Extensions.Options;

namespace PuntoMarket.Gateway.Auth;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddLoginServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));
        services.Configure<UsuariosOptions>(configuration.GetSection(UsuariosOptions.SectionName));

        services.AddHttpClient<KeycloakClient>((provider, client) =>
            client.Timeout = TimeSpan.FromSeconds(
                provider.GetRequiredService<IOptions<KeycloakOptions>>().Value.TimeoutSeconds));

        services.AddHttpClient<UsuariosClient>((provider, client) =>
        {
            var settings = provider.GetRequiredService<IOptions<UsuariosOptions>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        services.AddScoped<LoginService>();
        return services;
    }
}
