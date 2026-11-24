using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ActualizarUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CambiarEstadoUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CambiarPassword;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ConsultarCambioObligatorio;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CrearUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ObtenerPerfil;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ObtenerUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ReenviarAcceso;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.VerificarPasswordActual;

namespace PuntoMarket.Usuarios.Application.DependencyInjection;

public static class RegistroGestionDeUsuarios
{
    public static IServiceCollection AgregarGestionDeUsuarios(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios.AddScoped<ListarUsuariosCasoDeUso>();
        servicios.AddScoped<ObtenerUsuarioCasoDeUso>();
        servicios.AddScoped<ObtenerPerfilCasoDeUso>();
        servicios.AddScoped<CrearUsuarioCasoDeUso>();
        servicios.AddScoped<ActualizarUsuarioCasoDeUso>();
        servicios.AddScoped<CambiarEstadoUsuarioCasoDeUso>();
        servicios.AddScoped<CambiarPasswordCasoDeUso>();
        servicios.AddScoped<ReenviarAccesoCasoDeUso>();
        servicios.AddScoped<VerificarPasswordActualCasoDeUso>();
        servicios.AddScoped<ConsultarCambioObligatorioCasoDeUso>();

        return servicios;
    }
}
