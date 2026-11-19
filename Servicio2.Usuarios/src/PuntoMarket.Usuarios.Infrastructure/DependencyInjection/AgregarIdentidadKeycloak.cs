using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Usuarios.Infrastructure.Identity;

namespace PuntoMarket.Usuarios.Infrastructure.DependencyInjection;

public static class RegistroIdentidadKeycloak
{
    public static IServiceCollection AgregarIdentidadKeycloak(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        servicios.AddSingleton(new Lazy<KeycloakOpciones>(() => CargarOpciones(configuracion)));

        var segundosDeEspera = configuracion.GetValue(
            $"{KeycloakOpciones.Seccion}:{nameof(KeycloakOpciones.TimeoutSeconds)}",
            new KeycloakOpciones().TimeoutSeconds);
        servicios.AddHttpClient<IGestorDeIdentidad, KeycloakGestorDeIdentidad>(
            cliente => cliente.Timeout = TimeSpan.FromSeconds(segundosDeEspera));

        return servicios;
    }

    private static KeycloakOpciones CargarOpciones(IConfiguration configuracion)
    {
        var opciones = new KeycloakOpciones();
        configuracion.GetSection(KeycloakOpciones.Seccion).Bind(opciones);
        opciones.Validar();
        return opciones;
    }
}
