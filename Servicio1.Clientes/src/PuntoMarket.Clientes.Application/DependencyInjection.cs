using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Clientes.Application.Ports.In;
using PuntoMarket.Clientes.Application.UseCases;

namespace PuntoMarket.Clientes.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso. No depende de ningún adaptador: los puertos de salida se resuelven en la raíz de composición (API).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICrearCliente, CrearCliente>();
        services.AddScoped<IActualizarCliente, ActualizarCliente>();
        services.AddScoped<IObtenerCliente, ObtenerCliente>();
        services.AddScoped<IListarClientes, ListarClientes>();
        services.AddScoped<IDesactivarCliente, DesactivarCliente>();
        services.AddScoped<IReactivarCliente, ReactivarCliente>();
        return services;
    }
}
