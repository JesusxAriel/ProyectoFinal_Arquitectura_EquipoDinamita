using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Frontend.Clientes;

namespace PuntoMarket.Frontend.Pages.Clientes;

[Authorize(Roles = "Administrador,Cajero")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class IndexModel(IClientesApiAdapter api) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Busqueda { get; set; }
    [BindProperty(SupportsGet = true)] public string Estado { get; set; } = "Todos";
    [BindProperty(SupportsGet = true)] public string OrdenarPor { get; set; } = "RazonSocial";
    [BindProperty(SupportsGet = true)] public bool Descendente { get; set; }
    [BindProperty(SupportsGet = true)] public int Pagina { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int TamanoPagina { get; set; } = 10;
    [BindProperty] public GuardarClienteFrontend Input { get; set; } = new("", "", null, null);
    public IReadOnlyList<ClienteFrontend> Items { get; private set; } = [];
    public long Total { get; private set; }
    public int TotalPaginas { get; private set; }
    public string? ErrorGestion { get; private set; }
    public Dictionary<string, string[]> ErroresApi { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool EsAdministrador => User.IsInRole("Administrador");

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var r = await api.ListarAsync(Busqueda, Estado, OrdenarPor, Descendente, Math.Max(1, Pagina), Math.Clamp(TamanoPagina, 1, 100), ct);
        if (!r.Exitoso) { ErrorGestion = r.Mensaje; Response.StatusCode = r.Codigo switch { "401" => 401, _ => 503 }; return Page(); }
        if (r.Valor is not null) { Items = r.Valor.Items; Total = r.Valor.Total; TotalPaginas = r.Valor.TotalPaginas; }
        return Page();
    }

    public async Task<IActionResult> OnGetEditarAsync(int id, CancellationToken ct)
    {
        var r = await api.ObtenerAsync(id, ct);
        if (!r.Exitoso) { ErrorGestion = r.Mensaje; return Page(); }
        if (r.Valor is not null) Input = new(r.Valor.NitCi, r.Valor.RazonSocial, r.Valor.Correo, r.Valor.Telefono);
        ViewData["EditarId"] = id;
        return await OnGetAsync(ct);
    }

    public async Task<IActionResult> OnPostGuardarAsync(int? id, CancellationToken ct)
    {
        ValidarCliente();
        if (!ModelState.IsValid) { Response.StatusCode = 400; return await OnGetAsync(ct); }
        var r = id is null ? await api.CrearAsync(Input, ct) : await api.ActualizarAsync(id.Value, Input, ct);
        if (!r.Exitoso)
        {
            ErrorGestion = r.Mensaje;
            ErroresApi = r.Errores.ToDictionary(x => x.Key, x => x.Value);
            foreach (var campo in ErroresApi)
            {
                var clave = campo.Key.Equals("nitCi", StringComparison.OrdinalIgnoreCase) ? nameof(Input.NitCi)
                    : campo.Key.Equals("razonSocial", StringComparison.OrdinalIgnoreCase) ? nameof(Input.RazonSocial)
                    : campo.Key.Equals("correo", StringComparison.OrdinalIgnoreCase) ? nameof(Input.Correo)
                    : nameof(Input.Telefono);
                foreach (var mensaje in campo.Value) ModelState.AddModelError(clave, mensaje);
            }
            Response.StatusCode = r.Codigo is "Cliente.NoEncontrado" ? 404 : r.Codigo?.Contains("Duplicado") == true ? 409 : 400;
            return await OnGetAsync(ct);
        }
        TempData["MensajeGestion"] = id is null ? "Cliente creado correctamente." : "Cliente actualizado correctamente.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBajaAsync(int id, bool confirmar, CancellationToken ct)
    {
        if (!EsAdministrador) { ErrorGestion = "Solo un administrador puede dar de baja clientes."; Response.StatusCode = 403; return await OnGetAsync(ct); }
        if (!confirmar) { ErrorGestion = "Debe confirmar la baja del cliente."; Response.StatusCode = 400; return await OnGetAsync(ct); }
        var r = await api.DesactivarAsync(id, ct);
        if (!r.Exitoso) { ErrorGestion = r.Mensaje; Response.StatusCode = r.Codigo == "Cliente.GenericoProtegido" ? 409 : 400; return await OnGetAsync(ct); }
        TempData["MensajeGestion"] = "Cliente dado de baja correctamente.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReactivarAsync(int id, CancellationToken ct)
    {
        if (!EsAdministrador) { ErrorGestion = "Solo un administrador puede reactivar clientes."; Response.StatusCode = 403; return await OnGetAsync(ct); }
        var r = await api.ReactivarAsync(id, ct);
        if (!r.Exitoso) { ErrorGestion = r.Mensaje; Response.StatusCode = 409; return await OnGetAsync(ct); }
        TempData["MensajeGestion"] = "Cliente reactivado correctamente.";
        return RedirectToPage();
    }

    private void ValidarCliente()
    {
        if (string.IsNullOrWhiteSpace(Input.RazonSocial)) ModelState.AddModelError(nameof(Input.RazonSocial), "La razón social es obligatoria.");
        else if (Input.RazonSocial.Trim().Length > 150) ModelState.AddModelError(nameof(Input.RazonSocial), "La razón social no puede superar los 150 caracteres.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(Input.NitCi?.Trim() ?? "", @"^[0-9]{5,15}$"))
            ModelState.AddModelError(nameof(Input.NitCi), "El NIT/CI debe tener entre 5 y 15 dígitos.");
        if (Input.NitCi?.Trim() == "0") ModelState.AddModelError(nameof(Input.NitCi), "El NIT/CI 0 está reservado al cliente genérico.");
        if (!string.IsNullOrWhiteSpace(Input.Telefono) && !System.Text.RegularExpressions.Regex.IsMatch(Input.Telefono.Trim(), @"^[467][0-9]{7}$"))
            ModelState.AddModelError(nameof(Input.Telefono), "El teléfono debe tener 8 dígitos y comenzar con 4, 6 o 7.");
        if (!string.IsNullOrWhiteSpace(Input.Correo))
        {
            try { _ = new System.Net.Mail.MailAddress(Input.Correo.Trim()); }
            catch { ModelState.AddModelError(nameof(Input.Correo), "El correo electrónico no tiene un formato válido."); }
        }
    }
}
