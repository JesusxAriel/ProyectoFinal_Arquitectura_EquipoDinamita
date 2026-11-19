namespace Proveedores.Application.Dtos;

public sealed record PaginaResultado<T>(IReadOnlyList<T> Items, int Pagina, int TamanoPagina, long Total)
{
    public int TotalPaginas => TamanoPagina <= 0 || Total <= 0 ? 0
        : (int)Math.Min(int.MaxValue, Total / TamanoPagina + (Total % TamanoPagina == 0 ? 0 : 1));
}
