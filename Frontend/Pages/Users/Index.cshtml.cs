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
    private const string MensajeReenviarPropio = "No puede reenviar el acceso a su propia cuenta.";

    public IReadOnlyList<UsuarioResponse> Usuarios { get; private set; } = [];
    public IReadOnlyList<RolResponse> Roles { get; private set; } = [];
    public UsuarioResponse? UsuarioEditando { get; private set; }
    public bool EsAdministrador => User.IsInRole("Administrador");
    public string? ErrorGestion { get; private set; }
    public bool AbrirModal { get; private set; }
    public bool EsEdicion => EditarId is > 0;

    [BindProperty]
    [Required(ErrorMessage = "El campo Nombres es obligatorio.")]
    [StringLength(100, ErrorMessage = "El campo Nombres no puede superar los 100 caracteres.")]
    public string Nombres { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = "El campo Apellido paterno es obligatorio.")]
    [StringLength(60, ErrorMessage = "El campo Apellido paterno no puede superar los 60 caracteres.")]
    public string ApellidoPaterno { get; set; } = "";

    [BindProperty]
    [StringLength(60, ErrorMessage = "El campo Apellido materno no puede superar los 60 caracteres.")]
    public string? ApellidoMaterno { get; set; }

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

    [TempData]
    public string? AdvertenciaGestion { get; set; }

    public string ClaseCampo(string campo, string clase = "form-control") =>
        ModelState.TryGetValue(campo, out var entrada) && entrada.Errors.Count > 0
            ? clase + " is-invalid"
            : clase;

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

    public async Task<IActionResult> OnPostNuevoAsync()
    {
        if (!EsAdministrador)
            return RedirigirSinPermisos();

        await CargarAsync();
        ModelState.Clear();
        AbrirModal = true;
        return Page();
    }

    public async Task<IActionResult> OnPostCrearAsync()
    {
        if (!EsAdministrador)
            return RedirigirSinPermisos();

        await CargarAsync();
        ValidarRol();
        if (!ModelState.IsValid)
        {
            AbrirModal = true;
            return Page();
        }

        var resultado = await api.Crear(new CreateUsuarioRequest(Nombres, ApellidoPaterno, ApellidoMaterno, Correo, RolId));
        if (resultado.IsFail)
        {
            ErrorGestion = resultado.Error;
            AbrirModal = true;
            return Page();
        }

        var creado = resultado.Value;
        if (creado.CorreoEnviado)
            AvisoGestion = $"Usuario {creado.Username} creado. Se envió un correo con sus credenciales de acceso a {Correo}.";
        else
            AdvertenciaGestion = $"Usuario {creado.Username} creado, pero no se pudo enviar el correo. Use Reenviar acceso.";

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
        Nombres = resultado.Value.Nombres;
        ApellidoPaterno = resultado.Value.ApellidoPaterno;
        ApellidoMaterno = resultado.Value.ApellidoMaterno;
        Correo = resultado.Value.Correo;
        RolId = Roles.FirstOrDefault(rol => rol.Nombre == resultado.Value.Rol)?.Id ?? 0;
        ModelState.Clear();
        AbrirModal = true;
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
        {
            AbrirModal = true;
            return Page();
        }

        var resultado = await api.Actualizar(
            EditarId.Value,
            new UpdateUsuarioRequest(Nombres, ApellidoPaterno, ApellidoMaterno, Correo, RolId));
        if (resultado.IsFail)
        {
            ErrorGestion = resultado.Error;
            AbrirModal = true;
            return Page();
        }

        AvisoGestion = "Cambios guardados correctamente.";
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
            ErrorGestion = MensajeReenviarPropio;
            await CargarAsync();
            return Page();
        }

        var resultado = await api.Reenviar(id);
        if (resultado.IsFail)
        {
            ErrorGestion = resultado.Error;
            await CargarAsync();
            return Page();
        }

        if (resultado.Value.CorreoEnviado)
            AvisoGestion = "Se envió un correo con un nuevo acceso temporal.";
        else
            AdvertenciaGestion = MensajeCorreoFallido;

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