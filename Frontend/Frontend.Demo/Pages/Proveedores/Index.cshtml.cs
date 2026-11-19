using Frontend.Demo.Models;
using Frontend.Demo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Demo.Pages.Proveedores;

// INTEGRACIÓN: Este PageModel pertenece solo a Proveedores. Crear Pages/<Modulo> y su cliente propio
//   para otros CRUD; reutilizar layout y estilos, conservando validación definitiva y autorización en la API.
public class IndexModel(ProveedoresApiClient api, DemoPerfilSession perfil) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Busqueda { get; set; }
    [BindProperty(SupportsGet = true)] public string Estado { get; set; } = "Activos";
    [BindProperty(SupportsGet = true)] public string OrdenarPor { get; set; } = "Nombre";
    [BindProperty(SupportsGet = true)] public bool Descendente { get; set; }
    [BindProperty(SupportsGet = true)] public int Pagina { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int TamanoPagina { get; set; } = 10;
    [BindProperty(SupportsGet = true)] public string? EditarId { get; set; }
    [BindProperty(SupportsGet = true)] public bool Nuevo { get; set; }
    [BindProperty] public ProveedorFormVm Form { get; set; } = new();
    [TempData] public string? Mensaje { get; set; }
    [TempData] public string? MensajeError { get; set; }
    public PaginaVm? Resultado { get; private set; }
    public bool EsAdministrador => perfil.EsAdministrador;
    public bool VerEliminados => Estado == "Inactivos";
    public bool AbrirModal { get; private set; }
    public string? ErrorActual { get; private set; }
    public IEnumerable<int> PaginasVisibles => Enumerable.Range(Math.Max(1, Pagina - 2),
        Math.Max(0, Math.Min(Resultado?.TotalPaginas ?? 0, Pagina + 2) - Math.Max(1, Pagina - 2) + 1));

    public async Task OnGetAsync(CancellationToken ct)
    {
        await CargarListadoAsync(ct);
        AbrirModal = Nuevo && EsAdministrador;
        if (!string.IsNullOrWhiteSpace(EditarId) && EsAdministrador)
        {
            var r = await api.ObtenerAsync(EditarId, ct);
            if (r.Ok && r.Data is { EstaActivo: true } p)
            {
                Form = new ProveedorFormVm { NombreEmpresa = p.NombreEmpresa, NumeroEmpresa = p.NumeroEmpresa,
                    CorreoReferencia = p.CorreoReferencia, EsAutogestionado = p.EsAutogestionado };
                AbrirModal = true;
            }
            else ErrorActual = r.Mensaje ?? "No se puede editar un proveedor eliminado. Reactívelo primero.";
        }
    }

    public async Task<IActionResult> OnPostCrearAsync(CancellationToken ct)
    {
        if (!EsAdministrador) return StatusCode(403);
        if (FormularioMalformado())
        {
            EditarId = null; AbrirModal = true;
            await CargarListadoAsync(ct); return Page();
        }
        var r = await api.CrearAsync(Form, ct);
        if (r.Ok) { Mensaje = "Proveedor añadido correctamente."; return RedirectToPage(new { Estado = "Activos", TamanoPagina, Pagina = 1 }); }
        ErrorActual = r.Mensaje;
        AgregarErrores(r.ErroresCampos);
        AbrirModal = true;
        EditarId = null;
        await CargarListadoAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostEditarAsync(string id, CancellationToken ct)
    {
        if (!EsAdministrador) return StatusCode(403);
        if (!IdEsValido(id)) return BadRequest("El identificador del proveedor no tiene un formato válido.");
        if (FormularioMalformado())
        {
            EditarId = id; AbrirModal = true;
            await CargarListadoAsync(ct); return Page();
        }
        var r = await api.ActualizarAsync(id, Form, ct);
        if (r.Ok) { Mensaje = "Cambios guardados correctamente."; return RedirectToPage(RutaActual()); }
        ErrorActual = r.Mensaje;
        AgregarErrores(r.ErroresCampos);
        EditarId = id;
        AbrirModal = true;
        await CargarListadoAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostDesactivarAsync(string id, CancellationToken ct)
    {
        if (!EsAdministrador) return StatusCode(403);
        if (!IdEsValido(id)) return BadRequest("El identificador del proveedor no tiene un formato válido.");
        var r = await api.DesactivarAsync(id, ct);
        if (r.Ok) Mensaje = "Proveedor movido a Eliminados. Puede reactivarlo cuando lo necesite.";
        else MensajeError = r.Mensaje;
        return RedirectToPage(RutaActual());
    }

    public async Task<IActionResult> OnPostReactivarAsync(string id, CancellationToken ct)
    {
        if (!EsAdministrador) return StatusCode(403);
        if (!IdEsValido(id)) return BadRequest("El identificador del proveedor no tiene un formato válido.");
        var r = await api.ReactivarAsync(id, ct);
        if (r.Ok)
        {
            Mensaje = "Proveedor reactivado. Ya está disponible en Activos.";
            return RedirectToPage(new { Estado = "Activos", TamanoPagina, Pagina = 1 });
        }
        MensajeError = r.Mensaje;
        return RedirectToPage(RutaActual());
    }

    public object RutaActual(int? pagina = null) => new { Busqueda, Estado, OrdenarPor, Descendente, TamanoPagina, Pagina = pagina ?? Pagina };
    private static bool IdEsValido(string? id) => id?.Length == 24 && id.All(Uri.IsHexDigit);
    private bool FormularioMalformado()
    {
        if (!ModelState.Any(e => e.Key.StartsWith("Form.", StringComparison.Ordinal) && e.Value?.Errors.Count > 0)) return false;
        ErrorActual = "Revise los tipos y formatos de los campos del formulario.";
        // Un checkbox no puede volver a representar una cadena inválida como booleano.
        // Se conserva el resto del formulario y no se envía ninguna escritura a la API.
        if (ModelState["Form.EsAutogestionado"]?.Errors.Count > 0) ModelState.Remove("Form.EsAutogestionado");
        return true;
    }
    private void AgregarErrores(IReadOnlyDictionary<string, string[]>? errores)
    {
        if (errores is null) return;
        foreach (var campo in new[] { "NombreEmpresa", "NumeroEmpresa", "CorreoReferencia" })
            if (errores.TryGetValue(char.ToLowerInvariant(campo[0]) + campo[1..], out var mensajes))
                foreach (var mensaje in mensajes) ModelState.AddModelError("Form." + campo, mensaje);
    }

    private async Task CargarListadoAsync(CancellationToken ct)
    {
        if (ModelState.Any(e => new[] { "Pagina", "TamanoPagina", "Descendente", "Nuevo" }.Contains(e.Key)
            && e.Value?.Errors.Count > 0))
        {
            ErrorActual ??= "Revise los parámetros de búsqueda y paginación.";
            return;
        }
        Busqueda = Busqueda?.Trim();
        if (Busqueda?.Length > 150) { ErrorActual ??= "La búsqueda no puede superar los 150 caracteres."; return; }
        Estado = Estado == "Inactivos" ? "Inactivos" : "Activos";
        TamanoPagina = TamanoPagina is 10 or 25 or 50 or 100 ? TamanoPagina : 10;
        Pagina = Math.Clamp(Pagina, 1, int.MaxValue / 100);
        OrdenarPor = "Nombre"; // La UI solo ofrece nombre A-Z / Z-A; el contrato de API conserva otros criterios.
        var r = await api.BuscarAsync(new ConsultaVm(Busqueda, Estado, OrdenarPor, Descendente, Pagina, TamanoPagina), ct);
        if (r.Ok && r.Data is { } datos)
        {
            if (Pagina > Math.Max(1, datos.TotalPaginas))
            {
                Pagina = Math.Max(1, datos.TotalPaginas);
                r = await api.BuscarAsync(new ConsultaVm(Busqueda, Estado, OrdenarPor, Descendente, Pagina, TamanoPagina), ct);
            }
            if (r.Ok) Resultado = r.Data;
        }
        if (!r.Ok || r.Data is null) ErrorActual ??= r.Mensaje ?? "No se pudo cargar el listado.";
    }
}
