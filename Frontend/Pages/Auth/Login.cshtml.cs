using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Pages.Auth;

public sealed class LoginModel(IAuthApiAdapter authApi, ISesionUsuario sesion) : PageModel
{
    [BindProperty]
    public string Usuario { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public string? MensajeError { get; private set; }

    [TempData]
    public string? MensajeActivacion { get; set; }

    public void OnGet() => sesion.Cerrar();

    public async Task<IActionResult> OnPostAsync()
    {
        var validacion = ValidadorCredenciales.Validar(Usuario, Password);
        if (validacion.IsFail)
        {
            return MostrarError(validacion.Error);
        }

        var login = await authApi.Login(new LoginRequest(Usuario.Trim(), Password));
        if (login.IsFail)
        {
            return MostrarError(login.Error);
        }

        sesion.Iniciar(login.Value);
        return RedirectToPage(login.Value.DebeCambiarPassword
            ? RutasAuth.CambiarPasswordObligatorio
            : RutasAuth.Inicio);
    }

    private PageResult MostrarError(string mensaje)
    {
        MensajeError = mensaje;
        Password = "";
        return Page();
    }
}
