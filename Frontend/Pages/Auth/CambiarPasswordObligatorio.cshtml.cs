using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Pages.Auth;

public sealed class CambiarPasswordObligatorioModel(ISesionUsuario sesion) : PageModel
{
    public IActionResult OnGet()
    {
        var datos = sesion.Actual;
        if (datos is null)
        {
            return RedirectToPage(RutasAuth.Login);
        }

        return datos.DebeCambiarPassword ? Page() : RedirectToPage(RutasAuth.Inicio);
    }
}
