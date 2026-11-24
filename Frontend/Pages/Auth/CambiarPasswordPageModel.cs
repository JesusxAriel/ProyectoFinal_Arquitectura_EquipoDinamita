using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Frontend.Auth;
using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Pages.Auth;

public abstract class CambiarPasswordPageModel(ISesionUsuario sesion, IPerfilApiAdapter perfilApi) : PageModel
{
    protected ISesionUsuario Sesion { get; } = sesion;

    [BindProperty]
    [Required(ErrorMessage = MensajesPassword.ActualObligatoria)]
    public string PasswordActual { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = MensajesPassword.NuevaObligatoria)]
    [PoliticaPassword]
    public string PasswordNueva { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = MensajesPassword.RepeticionObligatoria)]
    [Compare(nameof(PasswordNueva), ErrorMessage = MensajesPassword.RepeticionDistinta)]
    public string RepetirPasswordNueva { get; set; } = "";

    public string ClaseCampo(string campo) =>
        ModelState.TryGetValue(campo, out var entrada) && entrada.Errors.Count > 0
            ? "form-control is-invalid"
            : "form-control";

    public async Task<IActionResult> OnPostVerificarActualAsync()
    {
        if (Sesion.Actual is null)
            return Unauthorized();

        var actual = PasswordActual;
        ModelState.Clear();
        PasswordActual = "";

        if (string.IsNullOrEmpty(actual))
            return new JsonResult(new { estado = "incorrecta", mensaje = MensajesAuth.PasswordActualIncorrecta });

        var resultado = await perfilApi.VerificarPasswordActual(actual);
        if (resultado.IsFail)
            return new JsonResult(new { estado = "no-disponible", mensaje = resultado.Error }) { StatusCode = StatusCodes.Status503ServiceUnavailable };

        return resultado.Value
            ? new JsonResult(new { estado = "correcta", mensaje = MensajesPassword.ActualCorrecta })
            : new JsonResult(new { estado = "incorrecta", mensaje = MensajesAuth.PasswordActualIncorrecta });
    }

    protected async Task<IActionResult> ProcesarCambioPasswordAsync()
    {
        var request = new CambiarPasswordRequest(PasswordActual, PasswordNueva, RepetirPasswordNueva);
        ModelState.Clear();
        PasswordActual = "";
        PasswordNueva = "";
        RepetirPasswordNueva = "";

        var errores = ValidadorPassword.ValidarCampos(request.Actual, request.Nueva, request.Confirmacion);
        if (errores.Count > 0)
        {
            foreach (var error in errores)
                ModelState.AddModelError(error.Campo, error.Mensaje);

            return Page();
        }

        var resultado = await perfilApi.CambiarPassword(request);
        if (resultado.IsFail)
        {
            var campo = resultado.Error == MensajesAuth.PasswordActualIncorrecta
                ? ValidadorPassword.CampoActual
                : string.Empty;
            ModelState.AddModelError(campo, resultado.Error);
            return Page();
        }

        Sesion.MarcarPasswordCambiada();
        return RedirectToPage(RutasAuth.Inicio);
    }
}
