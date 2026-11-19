using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.DarDeBajaUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;

namespace PuntoMarket.Usuarios.Application.DependencyInjection;

public static class RegistroGestionDeUsuarios
{
    public static IServiceCollection AgregarGestionDeUsuarios(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios.AddScoped<ListarUsuariosCasoDeUso>();
        servicios.AddScoped<DarDeBajaUsuarioCasoDeUso>();

        return servicios;
    }
}
