using PuntoMarket.Clientes.Domain.Common;

namespace PuntoMarket.Clientes.Tests;

public class ResultTests
{
    [Fact]
    public void Success_conserva_el_valor_y_no_tiene_error()
    {
        var r = Result.Success(42);
        Assert.True(r.IsSuccess);
        Assert.False(r.IsFailure);
        Assert.Equal(42, r.Value);
        Assert.Equal(Error.None, r.Error);
    }

    [Fact]
    public void Failure_conserva_el_error_y_leer_Value_es_un_error_de_programacion()
    {
        var error = Error.Validation("X.Y", "mensaje");
        var r = Result.Failure<int>(error);
        Assert.True(r.IsFailure);
        Assert.Equal(error, r.Error);
        Assert.Throws<InvalidOperationException>(() => r.Value);
    }

    [Fact]
    public void Un_fallo_sin_error_no_es_valido()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Failure(Error.None));
    }
}
