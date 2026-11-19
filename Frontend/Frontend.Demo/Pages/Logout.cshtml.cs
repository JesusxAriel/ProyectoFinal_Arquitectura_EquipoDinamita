using Frontend.Demo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Demo.Pages;

public class LogoutModel(DemoPerfilSession perfil) : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");
    public IActionResult OnPost()
    {
        perfil.CerrarSesion();
        return RedirectToPage("/Login");
    }
}
