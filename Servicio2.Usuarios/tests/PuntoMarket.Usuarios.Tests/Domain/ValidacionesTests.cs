using PuntoMarket.Usuarios.Domain;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Domain;

public class ValidacionesTests
{
    private const string Campo = "El correo";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NoVacio_rechaza_valores_sin_contenido(string? valor)
    {
        var resultado = Validaciones.NoVacio(valor, Campo);

        Assert.True(resultado.IsFail);
        Assert.Equal("El correo es obligatorio.", resultado.Error);
    }

    [Fact]
    public void LongitudMaxima_mide_el_valor_con_los_espacios_normalizados()
    {
        Assert.True(Validaciones.LongitudMaxima("  ab  ", 2, Campo).IsOk);
        Assert.True(Validaciones.LongitudMaxima("abc", 2, Campo).IsFail);
    }

    [Theory]
    [InlineData("ale@correo.com")]
    [InlineData("  ALE@correo.com  ")]
    [InlineData("nombre@dominio.com")]
    [InlineData("nombre@dominio.edu.bo")]
    [InlineData("nombre.apellido+tag@sub-dominio.example.org")]
    public void Correo_acepta_formatos_validos(string valor)
    {
        Assert.True(Validaciones.Correo(valor, Campo).IsOk);
    }

    [Theory]
    [InlineData("ale@correo")]
    [InlineData("correo@gmail")]
    [InlineData("ale@.com")]
    [InlineData("ale@correo.")]
    [InlineData("ale@correo..com")]
    [InlineData("ale@-correo.com")]
    [InlineData("ale@correo.c")]
    [InlineData("@correo.com")]
    [InlineData("ale.correo.com")]
    [InlineData("ale@@correo.com")]
    [InlineData("ale correo@dominio.com")]
    [InlineData(null)]
    public void Correo_rechaza_formatos_invalidos(string? valor)
    {
        Assert.True(Validaciones.Correo(valor, Campo).IsFail);
    }

    [Fact]
    public void Correo_invalido_devuelve_el_mensaje_con_ejemplo()
    {
        var resultado = Validaciones.Correo("correo@gmail", Campo);

        Assert.True(resultado.IsFail);
        Assert.Equal("Ingrese un correo válido, por ejemplo nombre@dominio.com.", resultado.Error);
    }

    [Fact]
    public void SinCaracteresDeControl_rechaza_saltos_de_linea()
    {
        Assert.True(Validaciones.SinCaracteresDeControl("Ana\nMaria", Campo).IsFail);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Positivo_rechaza_valores_no_positivos(int valor)
    {
        Assert.True(Validaciones.Positivo(valor, Campo).IsFail);
    }

    [Fact]
    public void PrimerError_devuelve_el_primer_fallo_y_no_los_siguientes()
    {
        var resultado = Validaciones.PrimerError(
            Result.Ok(),
            Result.Fail("Primer error."),
            Result.Fail("Segundo error."));

        Assert.True(resultado.IsFail);
        Assert.Equal("Primer error.", resultado.Error);
    }

    [Fact]
    public void PrimerError_es_correcto_cuando_todas_las_validaciones_pasan()
    {
        Assert.True(Validaciones.PrimerError(Result.Ok(), Result.Ok()).IsOk);
    }
}
