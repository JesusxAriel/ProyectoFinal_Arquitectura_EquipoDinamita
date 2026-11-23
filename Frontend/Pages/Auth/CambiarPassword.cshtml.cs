using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Frontend.Auth;
using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Pages.Auth;

public sealed class CambiarPasswordModel(ISesionUsuario sesion, IPerfilApiAdapter perfilApi) : PageModel
{
    [BindProperty]
    public string PasswordActual { get; set; } = "";

    [BindProperty]
    public string PasswordNueva { get; set; } = "";

    [BindProperty]
    public string RepetirPasswordNueva { get; set; } = "";

    public IActionResult OnGet()
    {
        var datos = sesion.Actual;
        if (datos is null)
            return RedirectToPage(RutasAuth.Login);

        return datos.DebeCambiarPassword ? RedirectToPage(RutasAuth.CambiarPasswordObligatorio) : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var datos = sesion.Actual;
        if (datos is null)
            return RedirectToPage(RutasAuth.Login);
        if (datos.DebeCambiarPassword)
            return RedirectToPage(RutasAuth.CambiarPasswordObligatorio);

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

        sesion.MarcarPasswordCambiada();
        return RedirectToPage(RutasAuth.Inicio);
    }
}
