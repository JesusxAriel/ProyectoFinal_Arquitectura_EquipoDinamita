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

    protected async Task<IActionResult> ProcesarCambioPasswordAsync()
    {
        var request = new CambiarPasswordRequest(PasswordActual, PasswordNueva, RepetirPasswordNueva);
        ModelState.Clear();
        PasswordActual = "";
        PasswordNueva = "";
        RepetirPasswordNueva = "";

        var validacion = ValidadorPassword.Validar(request.Actual, request.Nueva, request.Confirmacion);
        if (validacion.IsFail)
        {
            ModelState.AddModelError(string.Empty, validacion.Error);
            return Page();
        }

        var resultado = await perfilApi.CambiarPassword(request);
        if (resultado.IsFail)
        {
            ModelState.AddModelError(string.Empty, resultado.Error);
            return Page();
        }

        Sesion.MarcarPasswordCambiada();
        return RedirectToPage(RutasAuth.Inicio);
    }
}
