using Microsoft.Extensions.Caching.Memory;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Tests.Auth;

public class ActivacionesPendientesTests
{
    private static ActivacionesPendientes Crear(TimeSpan? vigencia = null) =>
        new(new MemoryCache(new MemoryCacheOptions()), vigencia);

    [Fact]
    public void La_referencia_es_aleatoria_hexadecimal_de_64_caracteres_y_no_contiene_el_token()
    {
        var pendientes = Crear();

        var referencia = pendientes.Guardar("tok-secreto");

        Assert.Equal(64, referencia.Length);
        Assert.Matches("^[0-9A-F]{64}$", referencia);
        Assert.DoesNotContain("tok-secreto", referencia);
    }

    [Fact]
    public void Cada_guardado_produce_una_referencia_distinta()
    {
        var pendientes = Crear();

        var referencias = Enumerable.Range(0, 100).Select(_ => pendientes.Guardar("tok")).Distinct().Count();

        Assert.Equal(100, referencias);
    }

    [Fact]
    public void Obtener_devuelve_el_token_guardado_y_eliminar_lo_borra()
    {
        var pendientes = Crear();
        var referencia = pendientes.Guardar("tok-secreto");

        Assert.Equal("tok-secreto", pendientes.Obtener(referencia));
        pendientes.Eliminar(referencia);

        Assert.Null(pendientes.Obtener(referencia));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
    [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
    public void Una_referencia_invalida_o_desconocida_no_devuelve_nada(string? referencia)
    {
        var pendientes = Crear();

        Assert.Null(pendientes.Obtener(referencia));
        pendientes.Eliminar(referencia);
    }

    [Fact]
    public void La_vigencia_predeterminada_es_de_60_minutos_igual_que_el_enlace()
    {
        Assert.Equal(TimeSpan.FromMinutes(60), Crear().Vigencia);
        Assert.Equal(TimeSpan.FromMinutes(60), ActivacionesPendientes.VigenciaPredeterminada);
    }

    [Fact]
    public async Task La_referencia_vence_pasada_la_vigencia()
    {
        var pendientes = Crear(TimeSpan.FromMilliseconds(100));
        var referencia = pendientes.Guardar("tok-secreto");

        await Task.Delay(500);

        Assert.Null(pendientes.Obtener(referencia));
    }

    [Fact]
    public void No_se_guarda_un_token_vacio()
    {
        var pendientes = Crear();

        Assert.ThrowsAny<ArgumentException>(() => pendientes.Guardar(" "));
    }
}
