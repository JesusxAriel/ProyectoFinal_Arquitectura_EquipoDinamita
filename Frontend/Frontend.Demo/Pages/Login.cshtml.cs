using System.Text.Json;
using Frontend.Demo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Demo.Pages;

// DEMO TEMPORAL: sin credenciales; reutiliza el emisor JWT y la sesión existentes.
public class LoginModel(DemoPerfilSession perfil, DemoTokenProvider tokens) : PageModel
{
    public string? ErrorActual { get; private set; }
    public IActionResult OnGet(bool cambiar = false) => perfil.TienePerfil && !cambiar ? RedirectToPage("/Index") : Page();
    public async Task<IActionResult> OnPostAsync(string perfilElegido, CancellationToken ct)
    {
        if (perfilElegido is not (DemoPerfilSession.Administrador or DemoPerfilSession.Cajero))
        {
            ErrorActual = "Seleccione uno de los dos perfiles disponibles.";
            return Page();
        }
        try
        {
            var token = await tokens.ObtenerAsync(perfilElegido, ct);
            if (token is null) ErrorActual = "No se pudo iniciar la sesión de demostración. Compruebe el Gateway y la identidad de desarrollo.";
            else
            {
                perfil.Perfil = perfilElegido;
                return RedirectToPage("/Index");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            ErrorActual = "No se pudo conectar con la identidad de desarrollo. Intente nuevamente.";
        }
        return Page();
    }
}
