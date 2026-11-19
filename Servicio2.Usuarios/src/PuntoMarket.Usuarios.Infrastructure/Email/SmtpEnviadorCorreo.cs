using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Services;

namespace PuntoMarket.Usuarios.Infrastructure.Email;

public sealed class SmtpEnviadorCorreo(SmtpEnviadorCorreoOpciones opciones) : IEnviadorCorreo, IEnviadorCredenciales
{
    public const string AsuntoCredenciales = "Sus credenciales de acceso a PuntoMarket";
    public const string ErrorAlEnviarCredenciales = "No se pudo enviar el correo con las credenciales de acceso.";

    public async Task EnviarActivacionAsync(
        string correo,
        string token,
        DateTimeOffset expira,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(correo);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentNullException.ThrowIfNull(opciones);
        ValidarConfiguracionSmtp();

        if (string.IsNullOrWhiteSpace(opciones.BaseUrl)
            || !Uri.TryCreate(opciones.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var origen)
            || origen.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(origen.UserInfo)
            || !string.IsNullOrEmpty(origen.Query)
            || !string.IsNullOrEmpty(origen.Fragment))
        {
            throw new InvalidOperationException(
                "Configure BaseUrl con la URL pública HTTP/HTTPS del frontend, sin credenciales, query ni fragmento.");
        }

        var enlace = new Uri(origen, $"Users/Activar?token={Uri.EscapeDataString(token)}").AbsoluteUri;
        var enlaceHtml = WebUtility.HtmlEncode(enlace);
        var vencimiento = ZonaHorariaDeLaPaz.ALocal(expira.UtcDateTime).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

        using var mensaje = CrearMensaje(correo, "Active su cuenta de PuntoMarket", esHtml: true, $"""
                <p>Se creó una cuenta para usted en PuntoMarket.</p>
                <p>Para comenzar a utilizarla, debe definir su contraseña y activar la cuenta.</p>
                <p><a href="{enlaceHtml}">Definir contraseña y activar cuenta</a></p>
                <p>Este enlace tiene una vigencia limitada y vence el {vencimiento}.</p>
                <p>Si no esperaba este mensaje, puede ignorarlo.</p>
                """);

        await EnviarAsync(mensaje, cancellationToken);
    }

    public async Task<Result> EnviarCredenciales(
        string correo,
        string username,
        string passwordTemporal,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correo);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordTemporal);
        ValidarConfiguracionSmtp();

        try
        {
            using var mensaje = CrearMensaje(
                correo, AsuntoCredenciales, esHtml: false, ComponerCuerpoCredenciales(username, passwordTemporal));
            await EnviarAsync(mensaje, cancellationToken);
            return Result.Ok();
        }
        catch (Exception excepcion) when (excepcion is SmtpException or FormatException)
        {
            return Result.Fail(ErrorAlEnviarCredenciales);
        }
    }

    public static string ComponerCuerpoCredenciales(string username, string passwordTemporal) =>
        $"""
        Se creó una cuenta para usted en PuntoMarket.

        Nombre de usuario: {username}
        Contraseña temporal: {passwordTemporal}

        Al iniciar sesión por primera vez, el sistema le pedirá que cambie esta contraseña.
        Por su seguridad, no comparta estos datos con otras personas.

        Si no esperaba este mensaje, puede ignorarlo.
        """;

    private void ValidarConfiguracionSmtp()
    {
        if (string.IsNullOrWhiteSpace(opciones.Host)
            || string.IsNullOrWhiteSpace(opciones.FromAddress)
            || opciones.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "La configuración SMTP requiere Host, FromAddress y un Port válido.");
        }

        if (UsaUsuario() != !string.IsNullOrWhiteSpace(opciones.Password))
        {
            throw new InvalidOperationException(
                "La configuración SMTP debe incluir UserName y Password juntos.");
        }
    }

    private bool UsaUsuario() => !string.IsNullOrWhiteSpace(opciones.UserName);

    private MailMessage CrearMensaje(string correo, string asunto, bool esHtml, string cuerpo)
    {
        var mensaje = new MailMessage
        {
            From = new MailAddress(opciones.FromAddress, opciones.FromName),
            Subject = asunto,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = esHtml,
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
