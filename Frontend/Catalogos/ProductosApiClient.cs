namespace PuntoMarket.Frontend.Catalogos;

public interface IProductosApiClient
{
    Task<ResultadoOperacion<PaginaProductosVm>> ListarAsync(string? busqueda,string estado,string ordenarPor,bool descendente,int pagina,int tamano,CancellationToken ct);
    Task<ResultadoOperacion<ProductoVm>> ObtenerAsync(int id,CancellationToken ct);
    Task<ResultadoOperacion<CatalogosProductosVm>> CatalogosAsync(CancellationToken ct);
    Task<ResultadoOperacion<ProductoVm>> GuardarAsync(int? id,ProductoFormVm form,CancellationToken ct);
    Task<ResultadoOperacion<object>> DesactivarAsync(int id,CancellationToken ct);
}
public sealed class ProductosApiClient(HttpClient http) : IProductosApiClient
{
    public Task<ResultadoOperacion<PaginaProductosVm>> ListarAsync(string? b,string e,string o,bool d,int p,int t,CancellationToken ct) =>
        RespuestaGateway.Enviar<PaginaProductosVm>(http,HttpMethod.Get,$"api/productos?busqueda={Uri.EscapeDataString(b??"")}&estado={Uri.EscapeDataString(e)}&ordenarPor={Uri.EscapeDataString(o)}&descendente={d}&pagina={p}&tamanoPagina={t}",null,ct);
    public Task<ResultadoOperacion<ProductoVm>> ObtenerAsync(int id,CancellationToken ct) => RespuestaGateway.Enviar<ProductoVm>(http,HttpMethod.Get,$"api/productos/{id}",null,ct);
    public Task<ResultadoOperacion<CatalogosProductosVm>> CatalogosAsync(CancellationToken ct) => RespuestaGateway.Enviar<CatalogosProductosVm>(http,HttpMethod.Get,"api/productos/catalogos",null,ct);
    public Task<ResultadoOperacion<ProductoVm>> GuardarAsync(int? id,ProductoFormVm form,CancellationToken ct) =>
        RespuestaGateway.Enviar<ProductoVm>(http,id.HasValue?HttpMethod.Put:HttpMethod.Post,id.HasValue?$"api/productos/{id}":"api/productos",form,ct);
    public Task<ResultadoOperacion<object>> DesactivarAsync(int id,CancellationToken ct) => RespuestaGateway.Enviar<object>(http,HttpMethod.Delete,$"api/productos/{id}",null,ct);
}
