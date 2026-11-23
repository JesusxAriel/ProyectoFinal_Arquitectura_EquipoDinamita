using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Pages.Auth;

public sealed class CambiarPasswordModel(ISesionUsuario sesion, IPerfilApiAdapter perfilApi)
    : CambiarPasswordPageModel(sesion, perfilApi)
{
    public IActionResult OnGet()
    {
        var datos = Sesion.Actual;
        if (datos is null)
            return RedirectToPage(RutasAuth.Login);

        return datos.DebeCambiarPassword ? RedirectToPage(RutasAuth.CambiarPasswordObligatorio) : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var datos = Sesion.Actual;
        if (datos is null)
            return RedirectToPage(RutasAuth.Login);
        if (datos.DebeCambiarPassword)
            return RedirectToPage(RutasAuth.CambiarPasswordObligatorio);

        return await ProcesarCambioPasswordAsync();
    }
}
