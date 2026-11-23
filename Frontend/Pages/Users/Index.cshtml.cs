using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Frontend.Usuarios;

namespace PuntoMarket.Frontend.Pages.Users;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class IndexModel(IUsuariosApiAdapter api) : PageModel
{
    public IReadOnlyList<UsuarioResponse> Usuarios { get; private set; } = [];
    public IReadOnlyList<RolResponse> Roles { get; private set; } = [];
    public UsuarioResponse? UsuarioEditando { get; private set; }
    public string? ErrorGestion { get; private set; }
    public bool EsAdministrador => User.IsInRole("Administrador");

    [BindProperty, Required, StringLength(150)]
    public string Nombres { get; set; } = "";

    [BindProperty, Required, StringLength(150)]
    public string Apellidos { get; set; } = "";

    [BindProperty, Required, EmailAddress, StringLength(150)]
    public string Correo { get; set; } = "";

    [BindProperty, Range(1, int.MaxValue)]
    public int RolId { get; set; }

    [BindProperty]
    public int? EditarId { get; set; }

    [TempData]
    public string? MensajeGestion { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!EsAdministrador)
            return Forbid();

        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCrearAsync()
    {
        if (!EsAdministrador)
            return Forbid();

        await CargarAsync();
        ValidarRol();
        if (!ModelState.IsValid)
            return Page();

        var resultado = await api.Crear(new CreateUsuarioRequest(Nombres, Apellidos, Correo, RolId));
        if (resultado.IsFail)
        {
            ErrorGestion = resultado.Error;
            return Page();
        }

        MensajeGestion = "Usuario creado correctamente.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditarAsync(int id)
    {
        if (!EsAdministrador)
            return Forbid();
        if (id <= 0)
            return BadRequest();

        await CargarAsync();
        var resultado = await api.Obtener(id);
        if (resultado.IsFail)
        {
            ErrorGestion = resultado.Error;
            return Page();
        }

        EditarId = id;
        UsuarioEditando = resultado.Value;
        Correo = resultado.Value.Correo;
        RolId = Roles.FirstOrDefault(rol => rol.Nombre == resultado.Value.Rol)?.Id ?? 0;
        ModelState.Clear();
        return Page();
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        if (!EsAdministrador)
            return Forbid();
        if (EditarId is not > 0)
            return BadRequest();

        await CargarAsync();
        var usuario = await api.Obtener(EditarId.Value);
        if (usuario.IsFail)
        {
            ErrorGestion = usuario.Error;
            return Page();
        }

        UsuarioEditando = usuario.Value;
        ValidarRol();
        if (!ModelState.IsValid)
            return Page();

        var resultado = await api.Actualizar(
            EditarId.Value,
            new UpdateUsuarioRequest(Nombres, Apellidos, Correo, RolId));
        if (resultado.IsFail)
        {
            ErrorGestion = resultado.Error;
            return Page();
        }

        MensajeGestion = "Cambios guardados correctamente.";
        return RedirectToPage();
    }

    public IActionResult OnPostCancelarEdicion()
    {
        if (!EsAdministrador)
            return Forbid();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCambiarEstadoAsync(int id, bool activo)
    {
        if (!EsAdministrador)
            return Forbid();
        if (id <= 0)
            return BadRequest();

        var usuario = await api.Obtener(id);
        if (usuario.IsFail)
        {
            ErrorGestion = usuario.Error;
            await CargarAsync();
            return Page();
        }

        var usernameActual = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.Equals(usernameActual, usuario.Value.Username, StringComparison.OrdinalIgnoreCase))
        {
            ErrorGestion = "No puede cambiar el estado de su propia cuenta.";
            await CargarAsync();
            return Page();
        }

        var resultado = await api.CambiarEstado(id, new ChangeStatusRequest(activo));
        if (resultado.IsFail)
        {
            ErrorGestion = resultado.Error;
            await CargarAsync();
            return Page();
        }

        MensajeGestion = activo ? "Usuario activado correctamente." : "Usuario desactivado correctamente.";
        return RedirectToPage();
    }

    private void ValidarRol()
    {
        if (!Roles.Any(rol => rol.Id == RolId))
            ModelState.AddModelError(nameof(RolId), "Seleccione uno de los roles disponibles.");
    }

    private async Task CargarAsync()
    {
        var usuarios = await api.Listar();
        if (usuarios.IsOk)
            Usuarios = usuarios.Value;
        else
            ErrorGestion ??= usuarios.Error;

        var roles = await api.ListarRoles();
        if (roles.IsOk)
            Roles = roles.Value;
        else
            ErrorGestion ??= roles.Error;
    }
}
