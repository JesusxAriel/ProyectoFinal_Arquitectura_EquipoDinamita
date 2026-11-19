using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Proveedores.Application.Abstractions;
using Proveedores.Infrastructure.Integrations;
using Proveedores.Infrastructure.Mongo;

namespace Proveedores.Infrastructure;

public static class DependencyInjection
{
    // =====================================================
    // PATRÓN DE DISEÑO 4/4: SINGLETON (mediante el contenedor DI de ASP.NET Core)
    // MongoClient es thread-safe y administra su propio pool de conexiones: debe existir UNA
    // instancia por proceso. AddSingleton hace que el contenedor la cree una vez y la entregue
    // a todos los consumidores durante la vida de la aplicación (no un MongoClient por solicitud).
    // Es el ciclo de vida Singleton delegado al contenedor (no un Singleton clásico con
    // constructor privado y campo estático); es más testeable y seguro. La instancia es
    // por PROCESO: dos contenedores Docker tienen cada uno su propio MongoClient.
    // Se verifica en Proveedores.Tests/SingletonLifetimeTests.cs.
    // =====================================================
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // INTEGRACIÓN: Mongo:* configura exclusivamente la persistencia de Proveedores.
        //   Otros servicios conservan su base y credenciales; la comunicación entre ellos va por HTTP.
        services.Configure<MongoSettings>(configuration.GetSection(MongoSettings.SectionName));
        services.Configure<ProductosApiSettings>(configuration.GetSection(ProductosApiSettings.SectionName));

        services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(sp.GetRequiredService<IOptions<MongoSettings>>().Value.ConnectionString));

        services.AddSingleton<IMongoDatabase>(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(sp.GetRequiredService<IOptions<MongoSettings>>().Value.DatabaseName));

        services.AddSingleton<IMongoCollection<ProveedorDocument>>(sp =>
            sp.GetRequiredService<IMongoDatabase>().GetCollection<ProveedorDocument>(
                sp.GetRequiredService<IOptions<MongoSettings>>().Value.ProveedoresCollection));

        // ADAPTER de persistencia: el puerto de Application se resuelve con la implementación MongoDB.
        services.AddScoped<IProveedorRepository, MongoProveedorRepository>();

        // ADAPTER de la futura API de Productos (registrado pero NO usado por los casos de uso).
        services.AddHttpClient<IProductoApiClient, ProductoApiAdapter>((sp, client) =>
        {
            var s = sp.GetRequiredService<IOptions<ProductosApiSettings>>().Value;
            if (s.Enabled && Uri.TryCreate(s.BaseUrl, UriKind.Absolute, out var baseUri))
                client.BaseAddress = baseUri;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, s.TimeoutSegundos));
        });

        services.AddHostedService<MongoStartupInitializer>();
        return services;
    }
}
