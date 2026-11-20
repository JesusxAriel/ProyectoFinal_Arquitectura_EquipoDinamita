using PuntoMarket.Frontend.Catalogos;

using Microsoft.AspNetCore.Mvc;

namespace PuntoMarket.Frontend.Pages.Clientes;

// Presentación común: sesión del Grupo 2 y permisos por operación en servidor.
public class IndexModel(IClientesApiClient api) : PaginaCatalogo
{
    [BindProperty(SupportsGet = true)] public string? Busqueda { get; set; }
    [BindProperty(SupportsGet = true)] public string Estado { get; set; } = "Activos";
    [BindProperty(SupportsGet = true)] public string OrdenarPor { get; set; } = "RazonSocial";
    [BindProperty(SupportsGet = true)] public bool Descendente { get; set; }
    [BindProperty(SupportsGet = true)] public int Pagina { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int TamanoPagina { get; set; } = 10;
    [BindProperty] public int? EditarId { get; set; }
    [BindProperty(SupportsGet = true)] public bool Nuevo { get; set; }
    [BindProperty] public ClienteFormVm Form { get; set; } = new();
    [TempData] public string? Mensaje { get; set; }
    [TempData] public string? MensajeError { get; set; }
    public bool AbrirModal { get; private set; }
    public string? ErrorActual { get; private set; }
    public PaginaClientesVm? Resultado { get; private set; }
    public ClienteVm? Detalle { get; private set; }
    public bool VerEliminados => Estado == "Inactivos";
    public IEnumerable<int> PaginasVisibles => Enumerable.Range(Math.Max(1, Pagina - 2), Math.Max(0, Math.Min(Resultado?.TotalPaginas ?? 0, Pagina + 2) - Math.Max(1, Pagina - 2) + 1));

    public async Task OnGetAsync(CancellationToken ct)
    {
        // Los campos del formulario no son obligatorios al consultar listados.
        foreach (var k in ModelState.Keys.Where(k => k.StartsWith("Form.")).ToArray()) ModelState.Remove(k);
        await CargarAsync(ct);
        AbrirModal = Nuevo;
    }

    public async Task<IActionResult> OnPostAbrirEditarAsync(int id, CancellationToken ct)
    {
        if (!PuedeConsultar) return StatusCode(403);
        if (id <= 0 || ModelState["id"]?.Errors.Count > 0) return BadRequest("El identificador no es válido.");
        LimpiarValidacionFormulario();
        var r = await api.ObtenerAsync(id, ct);
        if (r.Ok && r.Data is { EstaActivo: true, EsGenerico: false } c)
        {
            EditarId = id; Form = new() { NitCi = c.NitCi, RazonSocial = c.RazonSocial, Correo = c.Correo, Telefono = c.Telefono };
            AbrirModal = true;
        }
        else ErrorActual = MensajeApi(r) ?? "No se puede editar un cliente genérico o desactivado.";
        await CargarAsync(ct); return Page();
    }
    public async Task<IActionResult> OnPostDetalleAsync(int id, CancellationToken ct)
    {
        if (!PuedeConsultar) return StatusCode(403);
        if (id <= 0 || ModelState["id"]?.Errors.Count > 0) return BadRequest("El identificador no es válido.");
        LimpiarValidacionFormulario();
        var r = await api.ObtenerAsync(id, ct);
        if (r.Ok) Detalle = r.Data; else ErrorActual = MensajeApi(r);
        await CargarAsync(ct); return Page();
    }

    public async Task<IActionResult> OnPostGuardarAsync(CancellationToken ct)
    {
        if (!PuedeConsultar) return StatusCode(403);
        NormalizarFiltrosOpcionales();
        // El setter del modelo ya normalizó antes de validar. Mostrar ese mismo valor
        // al reabrir el formulario sin eliminar errores ni los demás valores introducidos.
        ModelState.SetModelValue("Form.RazonSocial", Form.RazonSocial, Form.RazonSocial);
        if (EditarId is <= 0) return BadRequest("El identificador no es válido.");
        if (EditarId is { } id)
        {
            var anterior = await api.ObtenerAsync(id, ct);
            if (!anterior.Ok || anterior.Data is not { EstaActivo: true, EsGenerico: false })
            { ErrorActual = MensajeApi(anterior) ?? "No se puede editar un cliente genérico o desactivado."; await CargarAsync(ct); return Page(); }
        }
        if (!ModelState.IsValid) { AbrirModal = true; ErrorActual = "Revise los campos indicados."; await CargarAsync(ct); return Page(); }
        var r = EditarId is { } actual ? await api.ActualizarAsync(actual, Form, ct) : await api.CrearAsync(Form, ct);
        if (r.Ok) { Mensaje = EditarId is null ? "Cliente creado correctamente." : "Cambios guardados correctamente."; return RedirectToPage(RutaActual()); }
        ErrorActual = MensajeApi(r); AbrirModal = true;
        if (r.ErroresCampos is { } campos)
            foreach (var campo in new[] { "NitCi", "RazonSocial", "Correo", "Telefono" })
                if (campos.TryGetValue(char.ToLowerInvariant(campo[0]) + campo[1..], out var errores))
                    foreach (var error in errores) ModelState.AddModelError("Form." + campo, error);
        await CargarAsync(ct); return Page();
    }
    public Task<IActionResult> OnPostDesactivarAsync(int id, CancellationToken ct) => CambiarEstadoAsync(id, false, ct);
    public Task<IActionResult> OnPostReactivarAsync(int id, CancellationToken ct) => CambiarEstadoAsync(id, true, ct);
    private async Task<IActionResult> CambiarEstadoAsync(int id, bool reactivar, CancellationToken ct)
    {
        if (!EsAdministrador) return StatusCode(403);
        if (id <= 0 || ModelState["id"]?.Errors.Count > 0) return BadRequest("El identificador no es válido.");
        var actual = await api.ObtenerAsync(id, ct);
        if (!actual.Ok || actual.Data is null) {
            MensajeError = MensajeApi(actual);
            if (actual.Status is 401 or 403) { await CargarAsync(ct); return Page(); }
            return RedirectToPage(RutaActual());
        }
        if (actual.Data.EsGenerico) { MensajeError = "El cliente genérico Sin Nombre está protegido."; return RedirectToPage(RutaActual()); }
        var r = reactivar ? await api.ReactivarAsync(id, ct) : await api.DesactivarAsync(id, ct);
        if (r.Ok) Mensaje = reactivar ? "Cliente reactivado correctamente." : "Cliente movido a Eliminados. Sus datos se conservan.";
        else MensajeError = MensajeApi(r);
        if (r.Status is 401 or 403) { await CargarAsync(ct); return Page(); }
        return RedirectToPage(RutaActual());
    }
    public object RutaActual(int? pagina = null) => new { Busqueda, Estado, OrdenarPor, Descendente, TamanoPagina, Pagina = pagina ?? Pagina };
    private void NormalizarFiltrosOpcionales()
    {
        // Los filtros de navegación son opcionales; no son campos obligatorios del cliente.
        if (string.IsNullOrWhiteSpace(ModelState[nameof(Estado)]?.AttemptedValue)) { Estado = "Activos"; ModelState.Remove(nameof(Estado)); }
        if (string.IsNullOrWhiteSpace(ModelState[nameof(OrdenarPor)]?.AttemptedValue)) { OrdenarPor = "RazonSocial"; ModelState.Remove(nameof(OrdenarPor)); }
    }
    private async Task CargarAsync(CancellationToken ct)
    {
        NormalizarFiltrosOpcionales();
        if (ModelState.Any(e => new[] { "Pagina", "TamanoPagina", "Descendente", "EditarId", "Nuevo" }.Contains(e.Key) && e.Value?.Errors.Count > 0)) { ErrorActual ??= "Revise los parámetros de consulta."; return; }
        Busqueda = Busqueda?.Trim();
        if (Busqueda?.Length > 150 || Busqueda?.Any(char.IsControl) == true) { ErrorActual ??= "La búsqueda admite hasta 150 caracteres sin controles."; return; }
        if (Estado is not ("Activos" or "Inactivos" or "Todos") || OrdenarPor is not ("RazonSocial" or "NitCi" or "FechaCreacion" or "FechaActualizacion")) { ErrorActual ??= "El filtro u orden no es válido."; return; }
        TamanoPagina = TamanoPagina is 10 or 25 or 50 or 100 ? TamanoPagina : 10; Pagina = Math.Clamp(Pagina, 1, int.MaxValue / 100);
        var r = await api.ListarAsync(new(Busqueda, Estado, OrdenarPor, Descendente, Pagina, TamanoPagina), ct);
        if (r.Ok && r.Data is { } data && Pagina > Math.Max(1, data.TotalPaginas))
        { Pagina = Math.Max(1, data.TotalPaginas); r = await api.ListarAsync(new(Busqueda, Estado, OrdenarPor, Descendente, Pagina, TamanoPagina), ct); }
        if (r.Ok) Resultado = r.Data; else ErrorActual ??= MensajeApi(r);
    }
}
