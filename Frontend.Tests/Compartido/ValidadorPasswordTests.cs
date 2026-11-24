using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Tests.Compartido;

public sealed class ValidadorPasswordTests
{
    [Theory]
    [InlineData(null, "Nueva#12", "Nueva#12", "Ingrese su contraseña actual.")]
    [InlineData("", "Nueva#12", "Nueva#12", "Ingrese su contraseña actual.")]
    [InlineData("   ", "Nueva#12", "Nueva#12", "Ingrese su contraseña actual.")]
    [InlineData("Actual", null, "Nueva#12", "Ingrese la nueva contraseña.")]
    [InlineData("Actual", "", "Nueva#12", "Ingrese la nueva contraseña.")]
    [InlineData("Actual", "   ", "Nueva#12", "Ingrese la nueva contraseña.")]
    [InlineData("Actual", "Abcd#12", "Abcd#12", "La nueva contraseña debe tener al menos 8 caracteres.")]
    [InlineData("Actual", "nueva#12", "nueva#12", "La nueva contraseña debe incluir al menos una letra mayúscula.")]
    [InlineData("Actual", "NUEVA#12", "NUEVA#12", "La nueva contraseña debe incluir al menos una letra minúscula.")]
    [InlineData("Actual", "Nueva#aa", "Nueva#aa", "La nueva contraseña debe incluir al menos un número.")]
    [InlineData("Actual", "Nueva123", "Nueva123", "La nueva contraseña debe incluir al menos un carácter especial.")]
    [InlineData("Actual", "Nueva#12", null, "Repita la nueva contraseña.")]
    [InlineData("Actual", "Nueva#12", "", "Repita la nueva contraseña.")]
    [InlineData("Actual", "Nueva#12", "   ", "Repita la nueva contraseña.")]
    [InlineData("Actual", "Nueva#12", "Otra#123", "La repetición no coincide con la nueva contraseña.")]
    [InlineData("Actual", "Nueva#12", "nueva#12", "La repetición no coincide con la nueva contraseña.")]
    [InlineData("Actual", "Nueva#12", " Nueva#12 ", "La repetición no coincide con la nueva contraseña.")]
    public void Validar_Incumplimiento_DevuelveFailConMensaje(string? actual, string? nueva, string? confirmacion, string esperado)
    {
        var result = ValidadorPassword.Validar(actual, nueva, confirmacion);

        Assert.True(result.IsFail);
        Assert.Equal(esperado, result.Error);
    }

    [Theory]
    [InlineData("Abcd#1x", "La nueva contraseña debe tener al menos 8 caracteres.")]
    [InlineData("Abcd#12x", null)]
    [InlineData("abcd#12x", "La nueva contraseña debe incluir al menos una letra mayúscula.")]
    [InlineData("ABCD#12X", "La nueva contraseña debe incluir al menos una letra minúscula.")]
    [InlineData("Abcd#efg", "La nueva contraseña debe incluir al menos un número.")]
    [InlineData("Abcd12efg", "La nueva contraseña debe incluir al menos un carácter especial.")]
    [InlineData("abc", "La nueva contraseña debe tener al menos 8 caracteres.")]
    public void Validar_LongitudYReglas_CadaMensajeCorrespondeALaReglaQueFalla(string nueva, string? esperado)
    {
        var result = ValidadorPassword.Validar("Actual", nueva, nueva);

        Assert.Equal(esperado is null, result.IsOk);
        Assert.Equal(esperado ?? "", result.Error);
    }

    [Fact]
    public void Validar_ExactamenteOchoCaracteresQueCumplenTodo_NoDaErrorDeLongitud()
    {
        const string nueva = "Abcd#123";
        Assert.Equal(8, nueva.Length);

        Assert.Empty(ValidadorPassword.ValidarCampos("Actual", nueva, nueva));
    }

    [Fact]
    public void ValidarCampos_AsignaCadaMensajeASuCampo()
    {
        var errores = ValidadorPassword.ValidarCampos("", "corta", "");

        Assert.Equal(
        [
            new ErrorDeCampo("PasswordActual", "Ingrese su contraseña actual."),
            new ErrorDeCampo("PasswordNueva", "La nueva contraseña debe tener al menos 8 caracteres."),
            new ErrorDeCampo("RepetirPasswordNueva", "Repita la nueva contraseña.")
        ], errores);
    }

    [Fact]
    public void ValidarCampos_RepeticionDistinta_SeAsignaAlCampoDeRepeticion()
    {
        var errores = ValidadorPassword.ValidarCampos("Actual", "Nueva#12", "Otra#123");

        var error = Assert.Single(errores);
        Assert.Equal("RepetirPasswordNueva", error.Campo);
        Assert.Equal("La repetición no coincide con la nueva contraseña.", error.Mensaje);
    }

    [Theory]
    [InlineData("Actual", "Nueva#12")]
    [InlineData("Actual", "UnaNueva#12345")]
    [InlineData(" Actual ", " Nueva#12 ")]
    public void Validar_DatosValidos_DevuelveOk(string actual, string nueva)
    {
        var result = ValidadorPassword.Validar(actual, nueva, nueva);

        Assert.True(result.IsOk);
        Assert.Equal("", result.Error);
    }
}
