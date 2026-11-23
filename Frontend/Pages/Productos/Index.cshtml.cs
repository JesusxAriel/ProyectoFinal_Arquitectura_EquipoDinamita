using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Frontend.Catalogos;
namespace PuntoMarket.Frontend.Pages.Productos;

public class IndexModel(IProductosApiClient api,IProveedoresApiClient proveedores) : PaginaCatalogo
{
    [BindProperty(SupportsGet=true)] public string? Busqueda {get;set;}
    [BindProperty(SupportsGet=true)] public string Estado {get;set;}="Activos";
    [BindProperty(SupportsGet=true)] public string OrdenarPor {get;set;}="Nombre";
    [BindProperty(SupportsGet=true)] public bool Descendente {get;set;}
    [BindProperty(SupportsGet=true)] public int Pagina {get;set;}=1;
    [BindProperty(SupportsGet=true)] public int TamanoPagina {get;set;}=10;
    [BindProperty] public int? EditarId {get;set;}
    [BindProperty] public ProductoFormVm Form {get;set;}=new();
    [TempData] public string? Mensaje {get;set;}
    public string? ErrorActual {get;private set;}
    public bool MostrarFormulario {get;private set;}
    public Dictionary<string,string> Rutas(int? pagina=null,string? estado=null) => new() {
        ["Busqueda"]=Busqueda??"", ["Estado"]=estado??Estado, ["OrdenarPor"]=OrdenarPor,
        ["Descendente"]=Descendente.ToString(), ["TamanoPagina"]=TamanoPagina.ToString(), ["Pagina"]=(pagina??Pagina).ToString()
    };
    public decimal? PrecioOriginal {get;private set;}
    public ProductoVm? Detalle {get;private set;}
    public async Task<IActionResult> OnPostDetalleAsync(int id,CancellationToken ct) {
        if(!PuedeConsultar) return StatusCode(403);
        if(id<=0 || ModelState["id"]?.Errors.Count>0) return BadRequest("El identificador no es válido.");
        LimpiarValidacionFormulario();
        var r=await api.ObtenerAsync(id,ct);
        if(r.Ok) Detalle=r.Data; else ErrorActual=MensajeApi(r);
        await Cargar(ct); return Page();
    }
    public PaginaProductosVm? Resultado {get;private set;}
    public CatalogosProductosVm? Catalogos {get;private set;}
    public IReadOnlyList<ProveedorVm> Proveedores {get;private set;}=[];
    public async Task OnGetAsync(CancellationToken ct) { LimpiarValidacionFormulario(); await Cargar(ct); }
    public async Task<IActionResult> OnPostNuevoAsync(CancellationToken ct) {
        if(!EsAdministrador) return StatusCode(403);
        // An empty POST can bind Form with an empty prefix, producing bare field keys.
        // Only opening a new creation clears those errors; Guardar keeps full validation.
        var campos=typeof(ProductoFormVm).GetProperties().Select(p=>p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(var key in ModelState.Keys.Where(k=>k.Equals(nameof(Form),StringComparison.OrdinalIgnoreCase)
            || k.StartsWith("Form.",StringComparison.OrdinalIgnoreCase) || campos.Contains(k)
            || k.Equals(nameof(EditarId),StringComparison.OrdinalIgnoreCase)).ToArray()) ModelState.Remove(key);
        Form=new(); EditarId=null; PrecioOriginal=null;
        ModelState.SetModelValue("Form.Costo",string.Empty,string.Empty);
        ModelState.SetModelValue("Form.PrecioVenta",string.Empty,string.Empty);
        MostrarFormulario=true; await Cargar(ct); return Page();
    }
    public async Task<IActionResult> OnPostEditarAsync(int id,CancellationToken ct) {
        if(!EsAdministrador) return StatusCode(403);
        if(id<=0 || ModelState["id"]?.Errors.Count>0) return BadRequest("El identificador no es válido.");
        LimpiarValidacionFormulario();
        var r=await api.ObtenerAsync(id,ct);
        if(r.Ok && r.Data is {EstaActivo:true} p) {
            PrecioOriginal=p.PrecioVenta; EditarId=p.Id; Form=new(){Codigo=p.Codigo,Nombre=p.Nombre,Costo=p.Costo,PrecioVenta=p.PrecioVenta,CategoriaId=p.CategoriaId,EmpaqueId=p.EmpaqueId,ProveedorId=p.ProveedorId};
            MostrarFormulario=true;
        } else ErrorActual=MensajeApi(r)??"El producto está desactivado.";
        await Cargar(ct); return Page();
    }
    public async Task<IActionResult> OnPostGuardarAsync(CancellationToken ct) {
        if(!EsAdministrador) return StatusCode(403);
        if(EditarId is <=0) return BadRequest("El identificador no es válido.");
        if(EditarId is null) {
            // Keep validation attached to the rendered Form.* inputs even for an empty POST.
            foreach(var propiedad in typeof(ProductoFormVm).GetProperties()) {
                if(!ModelState.TryGetValue(propiedad.Name,out var entrada)) continue;
                var key="Form."+propiedad.Name;
                ModelState.SetModelValue(key,entrada.RawValue,entrada.AttemptedValue);
                foreach(var error in entrada.Errors) ModelState.AddModelError(key,error.ErrorMessage);
                ModelState.Remove(propiedad.Name);
            }
        }
        NormalizarFiltros();
        // MVC puede generar mensajes de conversión en inglés; la página mantiene sus errores en español.
        foreach(var entry in ModelState.Where(e=>e.Value?.Errors.Count>0).ToArray()) {
            entry.Value!.Errors.Clear();
            ModelState.AddModelError(entry.Key,"Revise el valor y el formato de este campo.");
        }
        if(EditarId is {} actual) {
            var anterior=await api.ObtenerAsync(actual,ct);
            if(anterior.Ok && anterior.Data is {} producto) PrecioOriginal=producto.PrecioVenta;
        }
        if(ModelState.IsValid) {
            var r=await api.GuardarAsync(EditarId,Form,ct);
            if(r.Ok) {Mensaje="Producto guardado correctamente.";return RedirectToPage(Rutas());}
            ErrorActual=MensajeApi(r);
            if(r.ErroresCampos is {} campos) foreach(var (campo,errores) in campos)
                foreach(var error in errores) ModelState.AddModelError("Form."+char.ToUpperInvariant(campo[0])+campo[1..],error);
        } else ErrorActual="Revise los datos del formulario.";
        MostrarFormulario=true; await Cargar(ct); return Page();
    }
    public async Task<IActionResult> OnPostDesactivarAsync(int id,CancellationToken ct) {
        if(!EsAdministrador) return StatusCode(403);
        if(id<=0 || ModelState["id"]?.Errors.Count>0) return BadRequest("El identificador no es válido.");
        LimpiarValidacionFormulario();
        var r=await api.DesactivarAsync(id,ct);
        if(r.Ok) {Mensaje="Producto desactivado. Sus datos se conservan.";return RedirectToPage(Rutas());}
        ErrorActual=MensajeApi(r); await Cargar(ct); return Page();
    }
    private async Task Cargar(CancellationToken ct) {
        NormalizarFiltros();
        Pagina=Math.Clamp(Pagina,1,int.MaxValue/100); TamanoPagina=Math.Clamp(TamanoPagina,1,100);
        var r=await api.ListarAsync(Busqueda,Estado,OrdenarPor,Descendente,Pagina,TamanoPagina,ct);
        if(r.Ok && r.Data is {} datos && Pagina>Math.Max(1,datos.TotalPaginas)) {
            Pagina=Math.Max(1,datos.TotalPaginas);
            r=await api.ListarAsync(Busqueda,Estado,OrdenarPor,Descendente,Pagina,TamanoPagina,ct);
        }
        if(r.Ok) Resultado=r.Data; else ErrorActual??=MensajeApi(r);
        if(!MostrarFormulario && Detalle is null) return;
        var c=await api.CatalogosAsync(ct);
        if(c.Ok) Catalogos=c.Data; else ErrorActual??=MensajeApi(c);
        var lista=new List<ProveedorVm>();
        for(var pagina=1;;pagina++) {
            var p=await proveedores.ListarAsync(new(null,Detalle is null ? "Activos" : "Todos","Nombre",false,pagina,100),ct);
            if(!p.Ok || p.Data is null) {ErrorActual??=MensajeApi(p)??"No se pudieron cargar los proveedores.";break;}
            lista.AddRange(p.Data.Items);
            if(pagina>=p.Data.TotalPaginas) break;
        }
        Proveedores=lista;
    }
    private void NormalizarFiltros() {
        if(string.IsNullOrWhiteSpace(ModelState[nameof(Estado)]?.AttemptedValue)) {Estado="Activos";ModelState.Remove(nameof(Estado));}
        if(string.IsNullOrWhiteSpace(ModelState[nameof(OrdenarPor)]?.AttemptedValue)) {OrdenarPor="Nombre";ModelState.Remove(nameof(OrdenarPor));}
    }
}
