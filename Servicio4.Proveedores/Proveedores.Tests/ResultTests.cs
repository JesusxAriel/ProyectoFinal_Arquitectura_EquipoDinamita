using Proveedores.Domain.Common;

namespace Proveedores.Tests;

public class ResultTests
{
    private static readonly Error ErrorDePrueba = Error.Validation("Prueba.Error", "Mensaje de prueba");

    [Fact]
    public void Success_SinValor_EsExitoso()
    {
        var r = Result.Success();
        Assert.True(r.IsSuccess);
        Assert.False(r.IsFailure);
        Assert.Equal(Error.None, r.Error);
    }

    [Fact]
    public void Failure_ConservaCodigoYMensaje()
    {
        var r = Result.Failure(ErrorDePrueba);
        Assert.True(r.IsFailure);
        Assert.False(r.IsSuccess);
        Assert.Equal("Prueba.Error", r.Error.Code);
        Assert.Equal("Mensaje de prueba", r.Error.Message);
        Assert.Equal(ErrorType.Validation, r.Error.Type);
    }

    [Fact]
    public void SuccessGenerico_DevuelveValor()
    {
        var r = Result.Success(42);
        Assert.True(r.IsSuccess);
        Assert.Equal(42, r.Value);
    }

    [Fact]
    public void FailureGenerico_LeerValueLanzaExcepcionTecnica()
    {
        var r = Result.Failure<int>(ErrorDePrueba);
        Assert.True(r.IsFailure);
        Assert.Equal(ErrorDePrueba, r.Error);
        Assert.Throws<InvalidOperationException>(() => r.Value);
    }

    [Fact]
    public void Failure_ConErrorNone_EsInvalido()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Failure(Error.None));
    }
}
