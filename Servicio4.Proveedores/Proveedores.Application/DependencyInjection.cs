using Microsoft.Extensions.DependencyInjection;
using Proveedores.Application.UseCases;

namespace Proveedores.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso. No depende de Infrastructure: los puertos se resuelven en la raíz de composición (API).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICrearProveedor, CrearProveedor>();
        services.AddScoped<IActualizarProveedor, ActualizarProveedor>();
        services.AddScoped<IObtenerProveedor, ObtenerProveedor>();
        services.AddScoped<IListarProveedores, ListarProveedores>();
        services.AddScoped<IDesactivarProveedor, DesactivarProveedor>();
        services.AddScoped<IReactivarProveedor, ReactivarProveedor>();
        return services;
    }
}
