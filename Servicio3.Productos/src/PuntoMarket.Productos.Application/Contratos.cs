using PuntoMarket.Productos.Domain;
namespace PuntoMarket.Productos.Application;

public sealed record CatalogoDto(int Id, string Nombre);
public sealed record CatalogosDto(IReadOnlyList<CatalogoDto> Categorias, IReadOnlyList<CatalogoDto> Empaques);
public sealed record PaginaResultado<T>(IReadOnlyList<T> Items, int Pagina, int TamanoPagina, long Total, int TotalPaginas);
public sealed record ListarProductosQuery(string? Busqueda = null, string Estado = "Activos", string OrdenarPor = "Nombre",
    bool Descendente = false, int Pagina = 1, int TamanoPagina = 10)
{
    public Result<ListarProductosQuery> Normalizar()
    {
        if (Busqueda?.Length > 150 || Busqueda?.Any(char.IsControl) == true ||
            Estado is not ("Activos" or "Inactivos" or "Todos") ||
            OrdenarPor is not ("Nombre" or "Codigo" or "Costo" or "PrecioVenta" or "FechaCreacion"))
            return Result.Failure<ListarProductosQuery>(Error.Validation("Producto.ConsultaInvalida", "Revise la búsqueda, el estado y la columna de ordenación."));
        var size = Math.Clamp(TamanoPagina, 1, 100);
        return Result.Success(this with { Busqueda = Busqueda?.Trim(), TamanoPagina = size, Pagina = Math.Clamp(Pagina, 1, int.MaxValue / size) });
    }
}
