using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Pages.Auth;

public sealed class LogoutModel(IAuthApiAdapter authApi, ISesionUsuario sesion) : PageModel
{
    public IActionResult OnGet() => RedirectToPage(RutasAuth.Login);

    public async Task<IActionResult> OnPostAsync()
    {
        await authApi.Logout();
        sesion.Cerrar();
        return RedirectToPage(RutasAuth.Login);
    }
}
