using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Pages.Auth;

public sealed class CambiarPasswordObligatorioModel(ISesionUsuario sesion) : PageModel
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
        {
            return RedirectToPage(RutasAuth.Login);
        }

        return datos.DebeCambiarPassword ? Page() : RedirectToPage(RutasAuth.Inicio);
    }

    public IActionResult OnPost()
    {
        var datos = sesion.Actual;
        if (datos is null)
        {
            return RedirectToPage(RutasAuth.Login);
        }

        if (!datos.DebeCambiarPassword)
        {
            return RedirectToPage(RutasAuth.Inicio);
        }

        // Las validaciones explícitas proporcionan mensajes en español incluso para campos omitidos.
        ModelState.Clear();
        if (string.IsNullOrEmpty(PasswordActual))
        {
            ModelState.AddModelError(nameof(PasswordActual), "Ingrese su contraseña actual.");
        }

        if (string.IsNullOrWhiteSpace(PasswordNueva))
        {
            ModelState.AddModelError(nameof(PasswordNueva), "Ingrese la nueva contraseña.");
        }
        else
        {
            if (PasswordNueva.Length < 8)
                ModelState.AddModelError(nameof(PasswordNueva), "La nueva contraseña debe tener al menos 8 caracteres.");
            if (!PasswordNueva.Any(char.IsUpper))
                ModelState.AddModelError(nameof(PasswordNueva), "La nueva contraseña debe incluir al menos una letra mayúscula.");
            if (!PasswordNueva.Any(char.IsLower))
                ModelState.AddModelError(nameof(PasswordNueva), "La nueva contraseña debe incluir al menos una letra minúscula.");
            if (!PasswordNueva.Any(char.IsDigit))
                ModelState.AddModelError(nameof(PasswordNueva), "La nueva contraseña debe incluir al menos un número.");
            if (!PasswordNueva.Any(caracter => !char.IsLetterOrDigit(caracter)))
                ModelState.AddModelError(nameof(PasswordNueva), "La nueva contraseña debe incluir al menos un carácter especial.");
        }

        if (string.IsNullOrEmpty(RepetirPasswordNueva))
        {
            ModelState.AddModelError(nameof(RepetirPasswordNueva), "Repita la nueva contraseña.");
        }
        else if (!string.Equals(PasswordNueva, RepetirPasswordNueva, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(RepetirPasswordNueva), "La repetición no coincide con la nueva contraseña.");
        }

        PasswordActual = "";
        PasswordNueva = "";
        RepetirPasswordNueva = "";

        if (ModelState.IsValid)
        {
            // Pendiente: invocar UsuariosApiAdapter.CambiarPassword cuando esté disponible la implementación correspondiente.
        }

        return Page();
    }
}
