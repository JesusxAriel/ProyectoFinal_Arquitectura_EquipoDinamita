using Proveedores.Application.Dtos;

namespace Proveedores.Tests;

public class PaginacionTests
{
    [Theory]
    [InlineData(int.MaxValue, 100)]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(int.MaxValue, 1)]
    [InlineData(int.MaxValue, 0)]
    [InlineData(int.MinValue, int.MinValue)]
    public void Normalizar_Limites_NoDesborda(int pagina, int tamano)
    {
        var q = new ListarProveedoresQuery { Pagina = pagina, TamanoPagina = tamano }.Normalizada();
        Assert.InRange(q.Desplazamiento, 0, int.MaxValue);
        Assert.Equal(((long)q.Pagina - 1) * q.TamanoPagina, q.Desplazamiento);
        Assert.InRange(q.TamanoPagina, 1, 100);
        Assert.Equal(q.Pagina, q.Normalizada().Pagina);
    }

    [Fact]
    public void TotalPaginas_NoDesbordaConTotalesGrandes()
    {
        Assert.Equal(int.MaxValue, new PaginaResultado<int>([], 1, 1, long.MaxValue).TotalPaginas);
        Assert.Equal(2, new PaginaResultado<int>([], 1, 10, 11).TotalPaginas);
        Assert.Equal(0, new PaginaResultado<int>([], 1, 10, 0).TotalPaginas);
    }
}
