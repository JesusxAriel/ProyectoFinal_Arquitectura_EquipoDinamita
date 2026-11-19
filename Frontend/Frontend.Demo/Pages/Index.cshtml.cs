using Frontend.Demo.Models;
using Frontend.Demo.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Demo.Pages;

public class IndexModel(ProveedoresApiClient api, DemoPerfilSession perfil) : PageModel
{
    public string PerfilActual => perfil.Perfil;
    public long? Total { get; private set; }
    public long? Activos { get; private set; }
    public long? Eliminados { get; private set; }
    public string? ErrorActual { get; private set; }
    public async Task OnGetAsync(CancellationToken ct)
    {
        var resultados = await Task.WhenAll(new[] { "Todos", "Activos", "Inactivos" }
            .Select(estado => api.ListarAsync(new ConsultaVm(null, estado, "Nombre", false, 1, 1), ct)));
        if (resultados.Any(r => !r.Ok || r.Data is null))
        {
            ErrorActual = resultados.First(r => !r.Ok || r.Data is null).Mensaje ?? "No se pudo obtener el resumen de proveedores.";
            return;
        }
        Total = resultados[0].Data!.Total;
        Activos = resultados[1].Data!.Total;
        Eliminados = resultados[2].Data!.Total;
    }
}
