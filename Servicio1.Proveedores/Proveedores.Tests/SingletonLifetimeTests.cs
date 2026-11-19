using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Proveedores.Application.Abstractions;
using Proveedores.Infrastructure;

namespace Proveedores.Tests;

/// <summary>Verifica el ciclo de vida Singleton de MongoClient sin conectarse a ninguna base de datos.</summary>
public class SingletonLifetimeTests
{
    private static ServiceProvider Proveedor()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mongo:ConnectionString"] = "mongodb://localhost:27017",
            ["Mongo:InitializeOnStartup"] = "false"
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(config);
        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public void MongoClient_EsLaMismaInstanciaEntreSolicitudesYScopes()
    {
        using var provider = Proveedor();

        IMongoClient a, b;
        using (var scope1 = provider.CreateScope()) a = scope1.ServiceProvider.GetRequiredService<IMongoClient>();
        using (var scope2 = provider.CreateScope()) b = scope2.ServiceProvider.GetRequiredService<IMongoClient>();

        Assert.Same(a, b);
        Assert.Same(a, provider.GetRequiredService<IMongoClient>());
    }

    [Fact]
    public void Repositorio_EsScoped_PeroComparteElMismoMongoClient()
    {
        using var provider = Proveedor();

        IProveedorRepository r1, r2;
        using (var s1 = provider.CreateScope()) r1 = s1.ServiceProvider.GetRequiredService<IProveedorRepository>();
        using (var s2 = provider.CreateScope()) r2 = s2.ServiceProvider.GetRequiredService<IProveedorRepository>();

        Assert.NotSame(r1, r2); // el adaptador es Scoped...
        Assert.Same(provider.GetRequiredService<IMongoClient>(), provider.GetRequiredService<IMongoClient>()); // ...el cliente es Singleton
    }

    [Fact]
    public void ElRegistroDeMongoClient_EsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(new ConfigurationBuilder().Build());

        var descriptor = services.Single(d => d.ServiceType == typeof(IMongoClient));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }
}
