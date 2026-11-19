using Microsoft.Extensions.Options;

namespace PuntoMarket.Frontend.Auth;

public sealed class GatewayApiOptions
{
    public const string SectionName = "GatewayApi";

    public string BaseUrl { get; set; } = string.Empty;
}

public static class AuthServiceCollectionExtensions
{
    private static readonly TimeSpan SessionIdleTimeout = TimeSpan.FromMinutes(30);

    public static IServiceCollection AddAutenticacionPorSesion(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GatewayApiOptions>(configuration.GetSection(GatewayApiOptions.SectionName));
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = SessionIdleTimeout;
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITokenProvider>(_ => TokenProvider.Instance);
        services.AddScoped<ISesionUsuario, SesionUsuario>();
        services.AddTransient<BearerTokenHandler>();
        services.AddGatewayAdapter<IAuthApiAdapter, AuthApiAdapter>();
        return services;
    }

    public static IHttpClientBuilder AddGatewayAdapter<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        return services
            .AddHttpClient<TInterface, TImplementation>((provider, client) =>
                client.BaseAddress = new Uri(
                    provider.GetRequiredService<IOptions<GatewayApiOptions>>().Value.BaseUrl))
            .AddHttpMessageHandler<BearerTokenHandler>();
    }

    public static IApplicationBuilder UseAutenticacionPorSesion(this IApplicationBuilder app)
    {
        app.UseMiddleware<SesionAutenticacionMiddleware>();
        app.UseMiddleware<CambioPasswordObligatorioMiddleware>();
        return app;
    }
}
