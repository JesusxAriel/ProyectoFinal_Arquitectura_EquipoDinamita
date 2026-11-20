using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Clientes.Application.Ports.In;
using PuntoMarket.Clientes.Application.UseCases;

namespace PuntoMarket.Clientes.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IListarClientes, ListarClientes>();
        services.AddScoped<IObtenerCliente, ObtenerCliente>();
        services.AddScoped<ICrearCliente, CrearCliente>();
        services.AddScoped<IActualizarCliente, ActualizarCliente>();
        services.AddScoped<IDesactivarCliente, DesactivarCliente>();
        services.AddScoped<IReactivarCliente, ReactivarCliente>();
        return services;
    }
}
