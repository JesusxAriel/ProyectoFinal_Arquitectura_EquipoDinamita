using Microsoft.Extensions.Configuration;

namespace PuntoMarket.Usuarios.Infrastructure.Email;

public sealed class SmtpEnviadorCorreoOpciones
{
    public const string Seccion = "Smtp";
    public const string SeccionFrontend = "Frontend";
    public const string UrlFrontendPorDefecto = "http://localhost:5100";
    public const string RutaLogin = "/Auth/Login";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FrontendBaseUrl { get; set; } = UrlFrontendPorDefecto;

    public string UrlLogin => FrontendBaseUrl.TrimEnd('/') + RutaLogin;

    public static SmtpEnviadorCorreoOpciones Desde(IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        var seccion = configuracion.GetSection(Seccion);

        return new SmtpEnviadorCorreoOpciones
        {
            Host = Texto(seccion, nameof(Host)),
            Port = int.TryParse(seccion[nameof(Port)], out var puerto) ? puerto : 0,
            FromAddress = Texto(seccion, nameof(FromAddress)),
            FromName = Texto(seccion, nameof(FromName)),
            UserName = Texto(seccion, nameof(UserName)),
            Password = seccion[nameof(Password)] ?? string.Empty,
            EnableSsl = !bool.TryParse(seccion[nameof(EnableSsl)], out var ssl) || ssl,
            FrontendBaseUrl = UrlBase(configuracion)
        };
    }

    private static string UrlBase(IConfiguration configuracion)
    {
        var valor = Texto(configuracion.GetSection(SeccionFrontend), "BaseUrl");
        return valor.Length == 0 ? UrlFrontendPorDefecto : valor;
    }

    private static string Texto(IConfigurationSection seccion, string clave) =>
        seccion[clave]?.Trim() ?? string.Empty;
}
