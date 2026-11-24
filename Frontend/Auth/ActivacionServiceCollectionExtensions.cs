using Microsoft.Extensions.Options;

namespace PuntoMarket.Frontend.Auth;

public static class ActivacionServiceCollectionExtensions
{
    public static IServiceCollection AddActivacionDeCuentas(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ActivacionesPendientes>();
        services
            .AddHttpClient<IActivacionApiAdapter, ActivacionApiAdapter>((provider, client) =>
                client.BaseAddress = new Uri(
                    provider.GetRequiredService<IOptions<GatewayApiOptions>>().Value.BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            });
        return services;
    }
}
