using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Frontend.Auth;
using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Pages.Auth;

public abstract class CambiarPasswordPageModel(ISesionUsuario sesion, IPerfilApiAdapter perfilApi) : PageModel
{
    protected ISesionUsuario Sesion { get; } = sesion;

    [BindProperty]
    public string PasswordActual { get; set; } = "";

    [BindProperty]
    public string PasswordNueva { get; set; } = "";

    [BindProperty]
    public string RepetirPasswordNueva { get; set; } = "";

    public string ClaseCampo(string campo) =>
        ModelState.TryGetValue(campo, out var entrada) && entrada.Errors.Count > 0
            ? "form-control is-invalid"
            : "form-control";

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
