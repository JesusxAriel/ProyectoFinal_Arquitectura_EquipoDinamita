using PuntoMarket.Usuarios.Domain.Services;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Domain;

public class GeneradorPasswordTemporalTests
{
    private const int Repeticiones = 200;

    [Fact]
    public void La_temporal_siempre_cumple_la_politica()
    {
        for (var intento = 0; intento < Repeticiones; intento++)
            Assert.True(PoliticaDePassword.Validar(GeneradorPasswordTemporal.Generar()).IsOk);
    }

    [Fact]
    public void La_temporal_tiene_al_menos_la_longitud_minima()
    {
        Assert.True(GeneradorPasswordTemporal.Generar().Length >= PoliticaDePassword.LongitudMinima);
    }

    [Fact]
    public void No_repite_la_misma_temporal_en_cada_llamada()
    {
        var generadas = new HashSet<string>();

        for (var intento = 0; intento < Repeticiones; intento++)
            generadas.Add(GeneradorPasswordTemporal.Generar());

        Assert.True(generadas.Count > Repeticiones / 2);
    }

    [Fact]
    public void No_usa_caracteres_que_se_confunden_al_leerlos()
    {
        for (var intento = 0; intento < Repeticiones; intento++)
            Assert.DoesNotContain(GeneradorPasswordTemporal.Generar(), caracter => caracter is 'l' or 'I' or 'O' or '0' or '1');
    }
}
