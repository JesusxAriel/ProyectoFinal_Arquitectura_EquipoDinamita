using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Adapters.Out.Persistence.Repositories;

namespace PuntoMarket.Clientes.Adapters.Out.Persistence;

public static class PersistenceDependencyInjection
{
    // PATRÓN DE DISEÑO 4/4: Singleton
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MySqlConnection")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:MySqlConnection.");
        services.AddSingleton(_ => new MySqlDataSource(connectionString));
        services.AddScoped<IClienteRepository, MySqlClienteRepository>();
        services.AddHostedService<MySqlStartupInitializer>();
        return services;
    }
}
