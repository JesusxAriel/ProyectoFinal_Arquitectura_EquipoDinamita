using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
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

        servicios.AddScoped<IUsuarioRepositorio, MySqlUsuarioRepositorio>();
        servicios.AddScoped<IUsuarioGestionRepositorio, MySqlUsuarioGestionRepositorio>();
        servicios.AddSingleton<SmtpEnviadorCorreoOpciones>(_ =>
        {
            var opciones = new SmtpEnviadorCorreoOpciones();
            configuracion.GetSection("Smtp").Bind(opciones);
            opciones.BaseUrl = configuracion["Activacion:BaseUrl"] ?? string.Empty;
            return opciones;
        });
        servicios.AddTransient<IEnviadorCorreo, SmtpEnviadorCorreo>();
        servicios.AddTransient<IEnviadorCredenciales, SmtpEnviadorCorreo>();
        servicios.AgregarIdentidadKeycloak(configuracion);

        return servicios;
    }
}
