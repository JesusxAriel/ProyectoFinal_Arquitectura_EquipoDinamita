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
    private const string MensajeSinPermisos = "No tiene permisos para ver esta sección.";
    private const string MensajeCorreoFallido = "No se pudo enviar el correo. Use Reenviar acceso.";

    public IReadOnlyList<UsuarioResponse> Usuarios { get; private set; } = [];
    public IReadOnlyList<RolResponse> Roles { get; private set; } = [];
    public UsuarioResponse? UsuarioEditando { get; private set; }
    public bool EsAdministrador => User.IsInRole("Administrador");
    public string? ErrorGestion { get; private set; }

    [BindProperty]
    [Required(ErrorMessage = "El campo Nombres es obligatorio.")]
    [StringLength(150, ErrorMessage = "El campo Nombres no puede superar los 150 caracteres.")]
    public string Nombres { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = "El campo Apellidos es obligatorio.")]
    [StringLength(150, ErrorMessage = "El campo Apellidos no puede superar los 150 caracteres.")]
    public string Apellidos { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = "El campo Correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El campo Correo no es una dirección de correo válida.")]
    [StringLength(150, ErrorMessage = "El campo Correo no puede superar los 150 caracteres.")]
    public string Correo { get; set; } = "";

    [BindProperty]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un rol válido.")]
    public int RolId { get; set; }

    [BindProperty]
    public int? EditarId { get; set; }

    [TempData]
    public string? AvisoGestion { get; set; }

    private IActionResult RedirigirSinPermisos()
    {
        TempData["MensajePermisos"] = MensajeSinPermisos;
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!EsAdministrador)
            return RedirigirSinPermisos();

        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCrearAsync()
    {
        if (!EsAdministrador)
            return RedirigirSinPermisos();

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

        var creado = resultado.Value;
        AvisoGestion = creado.CorreoEnviado
            ? $"Usuario {creado.Username} creado. Se envió un enlace de activación a {Correo}."
            : MensajeCorreoFallido;

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditarAsync(int id)
    {
        if (!EsAdministrador)
            return RedirigirSinPermisos();
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
            return RedirigirSinPermisos();
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

        AvisoGestion = "Cambios guardados correctamente.";
        return RedirectToPage();
    }

    public IActionResult OnPostCancelarEdicion()
    {
        if (!EsAdministrador)
            return RedirigirSinPermisos();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCambiarEstadoAsync(int id, bool activo)
    {
        if (!EsAdministrador)
            return RedirigirSinPermisos();
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

        AvisoGestion = activo ? "Usuario activado correctamente." : "Usuario desactivado correctamente.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReenviarAccesoAsync(int id)
    {
        if (!EsAdministrador)
            return RedirigirSinPermisos();
        if (id <= 0)
            return BadRequest();

        var resultado = await api.Reenviar(id);
        if (resultado.IsFail)
        {
            ErrorGestion = resultado.Error;
            await CargarAsync();
            return Page();
        }

        AvisoGestion = resultado.Value.CorreoEnviado
            ? "Se envió un nuevo enlace de activación."
            : MensajeCorreoFallido;

        return RedirectToPage();
    }

    private void ValidarRol()
    {
        if (ModelState.TryGetValue(nameof(RolId), out var entry) && entry.Errors.Count > 0)
        {
            entry.Errors.Clear();
            ModelState.AddModelError(nameof(RolId), "Seleccione un rol válido.");
            return;
        }

        if (RolId <= 0 || !Roles.Any(rol => rol.Id == RolId))
        {
            if (ModelState.TryGetValue(nameof(RolId), out var rolEntry))
                rolEntry.Errors.Clear();

            ModelState.AddModelError(nameof(RolId), "Seleccione uno de los roles disponibles.");
        }
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