using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using PuntoMarket.Clientes.Adapters.Out.Persistence.Initialization;
using PuntoMarket.Clientes.Adapters.Out.Persistence.Repositories;
using PuntoMarket.Clientes.Application.Ports.Out;

namespace PuntoMarket.Clientes.Adapters.Out.Persistence;

public static class DependencyInjection
{
    public const string ConnectionStringName = "MySqlConnection";

    // =====================================================
    // PATRÓN DE DISEÑO 4/4: SINGLETON (mediante el contenedor DI de ASP.NET Core)
    // MySqlDataSource administra el pool de conexiones de MySQL y es thread-safe: debe existir UNA
    // instancia por proceso. AddSingleton hace que el contenedor la cree una sola vez y la entregue a todos
    // los repositorios (que sí son Scoped, uno por solicitud). Es el ciclo de vida Singleton delegado al
    // contenedor, no un Singleton clásico con constructor privado y campo estático: es más testeable.
    // =====================================================
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var cadena = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(cadena))
            throw new InvalidOperationException(
                $"Falta la cadena de conexión 'ConnectionStrings:{ConnectionStringName}' (variable ConnectionStrings__{ConnectionStringName}).");

        services.Configure<ClientesDbOptions>(configuration.GetSection(ClientesDbOptions.SectionName));
        services.AddSingleton(_ => new MySqlDataSource(cadena));

        // ADAPTER de persistencia: el puerto de Application se resuelve con la implementación MySQL.
        services.AddScoped<IClienteRepository, MySqlClienteRepository>();
        services.AddHostedService<ClientesDatabaseInitializer>();
        return services;
    }
}
