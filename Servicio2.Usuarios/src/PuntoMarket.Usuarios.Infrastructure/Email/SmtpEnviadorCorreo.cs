using System.Net;
using System.Net.Mail;
using System.Text;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Infrastructure.Activacion;

namespace PuntoMarket.Usuarios.Infrastructure.Email;

public sealed class SmtpEnviadorCorreo(
    SmtpEnviadorCorreoOpciones opciones,
    ActivacionOpciones activacion) : IEnviadorCorreo
{
    public const string AsuntoActivacion = "Active su cuenta de PuntoMarket";
    public const string ErrorAlEnviarActivacion = "No se pudo enviar el correo de activación de la cuenta.";
    public const string UrlDeActivacionNoConfigurada =
        "El envío de correo no está configurado: la URL de activación debe ser una dirección HTTP o HTTPS válida, sin credenciales ni parámetros.";

    private const string RutaDeActivacion = "/Auth/Activar";

    public async Task<Result> EnviarActivacion(
        string correo,
        string username,
        string token,
        TimeSpan vigencia,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correo);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var errorConfiguracion = ValidarConfiguracionSmtp();
        if (errorConfiguracion is not null)
            return Result.Fail(errorConfiguracion);

        var enlace = ComponerEnlace(activacion.BaseUrl, token);
        if (enlace is null)
            return Result.Fail(UrlDeActivacionNoConfigurada);

        try
        {
            using var mensaje = CrearMensaje(
                correo, AsuntoActivacion, ComponerCuerpoActivacion(username, enlace, vigencia));
            await EnviarAsync(mensaje, cancellationToken);
            return Result.Ok();
        }
        catch (Exception excepcion) when (excepcion is SmtpException or FormatException)
        {
            return Result.Fail(ErrorAlEnviarActivacion);
        }
    }

    public static string? ComponerEnlace(string baseUrl, string token)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)
            || !Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var origen)
            || origen.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(origen.UserInfo)
            || !string.IsNullOrEmpty(origen.Query)
            || !string.IsNullOrEmpty(origen.Fragment))
        {
            return null;
        }

        return origen.GetLeftPart(UriPartial.Path).TrimEnd('/')
            + RutaDeActivacion + "?token=" + Uri.EscapeDataString(token);
    }

    public static string ComponerCuerpoActivacion(string username, string enlace, TimeSpan vigencia) =>
        $"""
        Se solicitó la activación de su cuenta en PuntoMarket.

        Nombre de usuario: {username}

        Para activar su cuenta y definir su contraseña, abra el siguiente enlace:

        {enlace}

        El enlace puede usarse una sola vez y vence en {DescribirVigencia(vigencia)}.
        Por su seguridad, no comparta este enlace con otras personas.

        Si no esperaba este mensaje, puede ignorarlo.
        """;

    public static string DescribirVigencia(TimeSpan vigencia)
    {
        var minutos = Math.Max(1, (int)Math.Ceiling(vigencia.TotalMinutes));

        if (minutos % 60 == 0)
        {
            var horas = minutos / 60;
            return horas == 1 ? "1 hora" : $"{horas} horas";
        }

        return minutos == 1 ? "1 minuto" : $"{minutos} minutos";
    }

    private string? ValidarConfiguracionSmtp()
    {
        if (string.IsNullOrWhiteSpace(opciones.Host)
            || string.IsNullOrWhiteSpace(opciones.FromAddress)
            || opciones.Port is < 1 or > 65535)
        {
            return "El envío de correo no está configurado: la configuración SMTP requiere Host, FromAddress y un Port válido.";
        }

        if (UsaUsuario() != !string.IsNullOrWhiteSpace(opciones.Password))
            return "La configuración SMTP es incompleta: debe incluir UserName y Password juntos.";

        return null;
    }

    private bool UsaUsuario() => !string.IsNullOrWhiteSpace(opciones.UserName);

    private MailMessage CrearMensaje(string correo, string asunto, string cuerpo)
    {
        var mensaje = new MailMessage
        {
            From = new MailAddress(opciones.FromAddress, opciones.FromName),
            Subject = asunto,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false,
            Body = cuerpo
        };
        mensaje.To.Add(new MailAddress(correo));
        return mensaje;
    }

    private async Task EnviarAsync(MailMessage mensaje, CancellationToken cancellationToken)
    {
        using var cliente = new SmtpClient(opciones.Host, opciones.Port)
        {
            EnableSsl = opciones.EnableSsl,
            UseDefaultCredentials = !UsaUsuario()
        };
        if (UsaUsuario())
        {
            cliente.Credentials = new NetworkCredential(opciones.UserName, opciones.Password);
        }

        await cliente.SendMailAsync(mensaje, cancellationToken);
    }
}
