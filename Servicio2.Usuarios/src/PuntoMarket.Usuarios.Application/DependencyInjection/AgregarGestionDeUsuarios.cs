using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CambiarPassword;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ObtenerPerfil;

namespace PuntoMarket.Usuarios.Application.DependencyInjection;

public static class RegistroGestionDeUsuarios
{
    public static IServiceCollection AgregarGestionDeUsuarios(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios.AddScoped<ListarUsuariosCasoDeUso>();
        servicios.AddScoped<ObtenerPerfilCasoDeUso>();
        servicios.AddScoped<CambiarPasswordCasoDeUso>();

        return servicios;
    }
}
