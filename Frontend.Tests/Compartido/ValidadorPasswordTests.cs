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
