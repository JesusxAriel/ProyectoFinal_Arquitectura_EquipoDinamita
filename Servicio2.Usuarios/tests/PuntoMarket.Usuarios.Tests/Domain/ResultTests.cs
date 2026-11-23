using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Domain;

public class ResultTests
{
    private const string Mensaje = "El correo no tiene un formato válido.";

    [Fact]
    public void Ok_es_correcto_y_no_lleva_mensaje_de_error()
    {
        var resultado = Result.Ok();

        Assert.True(resultado.IsOk);
        Assert.False(resultado.IsFail);
        Assert.Equal("", resultado.Error);
    }

    [Fact]
    public void Fail_guarda_el_mensaje_y_no_es_correcto()
    {
        var resultado = Result.Fail(Mensaje);

        Assert.True(resultado.IsFail);
        Assert.False(resultado.IsOk);
        Assert.Equal(Mensaje, resultado.Error);
    }

    [Fact]
    public void Ok_con_valor_entrega_el_valor_sin_error()
    {
        var resultado = Result<string>.Ok("jperez");

        Assert.True(resultado.IsOk);
        Assert.Equal("jperez", resultado.Value);
        Assert.Equal("", resultado.Error);
    }

    [Fact]
    public void Fail_con_valor_no_entrega_ningun_valor()
    {
        var resultado = Result<Usuario>.Fail(Mensaje);

        Assert.True(resultado.IsFail);
        Assert.Null(resultado.Value);
        Assert.Equal(Mensaje, resultado.Error);
    }

    [Fact]
    public void Fail_de_un_tipo_por_valor_devuelve_el_predeterminado()
    {
        var resultado = Result<int>.Fail(Mensaje);

        Assert.True(resultado.IsFail);
        Assert.Equal(0, resultado.Value);
    }

    [Fact]
    public void Fail_de_Result_generico_sigue_siendo_del_tipo_generico()
    {
        var resultado = Result<Usuario>.Fail(Mensaje);

        Assert.IsType<Result<Usuario>>(resultado);
    }

    [Fact]
    public void IsFail_siempre_es_lo_contrario_de_IsOk()
    {
        Assert.NotEqual(Result.Ok().IsOk, Result.Ok().IsFail);
        Assert.NotEqual(Result.Fail(Mensaje).IsOk, Result.Fail(Mensaje).IsFail);
    }
}
