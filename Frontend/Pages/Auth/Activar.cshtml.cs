using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Pages.Auth;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ActivarModel(IActivacionApiAdapter api, ActivacionesPendientes pendientes) : PageModel
{
    public const string NombreDeCookie = "PuntoMarket.Activacion";
    public const int LongitudMaximaDelToken = 4096;

    public string? MensajeError { get; private set; }

    public bool PuedeActivar { get; private set; }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        base.OnPageHandlerExecuting(context);
    }

    public IActionResult OnGet()
    {
        if (Request.Query.ContainsKey("token"))
            return GuardarTokenYRedirigir();

        var referencia = Request.Cookies[NombreDeCookie];
        if (pendientes.Obtener(referencia) is not null)
        {
            PuedeActivar = true;
            return Page();
        }

        return Rechazar(ReferenciaPerdida(referencia));
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var referencia = Request.Cookies[NombreDeCookie];
        var token = pendientes.Obtener(referencia);
        if (token is null)
            return Rechazar(ReferenciaPerdida(referencia));

        PuedeActivar = true;

        var formulario = await Request.ReadFormAsync(cancellationToken);
        var nueva = formulario["NuevaContrasena"].ToString();
        var confirmacion = formulario["ConfirmarContrasena"].ToString();

        if (string.IsNullOrWhiteSpace(nueva))
            return Fallo(MensajesActivacion.PasswordRequerida);

        if (string.IsNullOrWhiteSpace(confirmacion))
            return Fallo(MensajesActivacion.ConfirmacionRequerida);

        var resultado = await api.Activar(token, nueva, confirmacion, cancellationToken);
        if (resultado.IsFail)
        {
            if (resultado.Error == MensajesActivacion.EnlaceInvalido)
                return Rechazar(resultado.Error);

            return Fallo(resultado.Error);
        }

        LimpiarReferencia(referencia);
        TempData["MensajeActivacion"] = MensajesActivacion.ActivacionExitosa;
        return RedirectToPage("/Auth/Login");
    }

    private IActionResult GuardarTokenYRedirigir()
    {
        LimpiarReferencia(Request.Cookies[NombreDeCookie]);

        var token = Request.Query["token"];
        if (token.Count == 1
            && !string.IsNullOrWhiteSpace(token[0])
            && token[0]!.Length <= LongitudMaximaDelToken)
        {
            Response.Cookies.Append(NombreDeCookie, pendientes.Guardar(token[0]!.Trim()), OpcionesDeCookie());
        }

        return RedirectToPage();
    }

    private static string ReferenciaPerdida(string? referencia) =>
        ActivacionesPendientes.EsReferencia(referencia)
            ? MensajesActivacion.ReferenciaExpirada
            : MensajesActivacion.EnlaceInvalido;

    private PageResult Rechazar(string mensaje)
    {
        LimpiarReferencia(Request.Cookies[NombreDeCookie]);
        PuedeActivar = false;
        MensajeError = mensaje;
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return Page();
    }

    private PageResult Fallo(string mensaje)
    {
        PuedeActivar = true;
        MensajeError = mensaje;
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return Page();
    }

    private void LimpiarReferencia(string? referencia)
    {
        pendientes.Eliminar(referencia);
        if (referencia is not null)
            Response.Cookies.Delete(NombreDeCookie, OpcionesDeCookie());
    }

    private CookieOptions OpcionesDeCookie() => new()
    {
        HttpOnly = true,
        Secure = Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = Request.PathBase + "/Auth/Activar",
        IsEssential = true,
        MaxAge = pendientes.Vigencia
    };
}
