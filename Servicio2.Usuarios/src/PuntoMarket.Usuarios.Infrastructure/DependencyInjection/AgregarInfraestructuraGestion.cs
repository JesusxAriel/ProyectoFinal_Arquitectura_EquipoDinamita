using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Infrastructure.Activacion;
using PuntoMarket.Usuarios.Infrastructure.Email;
using PuntoMarket.Usuarios.Infrastructure.Persistence;
using PuntoMarket.Usuarios.Infrastructure.Security;
using PuntoMarket.Usuarios.Infrastructure.Time;

namespace PuntoMarket.Usuarios.Infrastructure.DependencyInjection;

public static class RegistroInfraestructuraGestion
{
    private const string NombreCadenaConexion = "MySqlConnection";

    public static IServiceCollection AgregarInfraestructuraGestion(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        servicios.TryAddSingleton(_ => new ConexionMySql(
            () => configuracion.GetConnectionString(NombreCadenaConexion)));
        servicios.TryAddSingleton<IReloj, RelojSistema>();
        servicios.TryAddSingleton<IGeneradorPassword, GeneradorPasswordSeguro>();
        servicios.TryAddSingleton<IGeneradorTokenActivacion, GeneradorTokenActivacion>();
        servicios.AddSingleton(_ => ActivacionOpciones.Desde(configuracion));
        servicios.AddSingleton(proveedor =>
            new ConfiguracionActivacion(proveedor.GetRequiredService<ActivacionOpciones>().Vigencia));

        servicios.AddScoped<IUsuarioRepositorio, MySqlUsuarioRepositorio>();
        servicios.AddScoped<IActivacionRepositorio, MySqlActivacionRepositorio>();
        servicios.AddSingleton(_ => SmtpEnviadorCorreoOpciones.Desde(configuracion));
        servicios.AddTransient<IEnviadorCorreo, SmtpEnviadorCorreo>();
        servicios.AgregarIdentidadKeycloak(configuracion);

        return servicios;
    }
}
