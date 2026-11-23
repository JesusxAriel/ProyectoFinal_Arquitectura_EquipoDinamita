namespace PuntoMarket.Frontend.Compartido;

public static class ValidadorPassword
{
    public static Result Validar(string? actual, string? nueva, string? confirmacion)
    {
        if (string.IsNullOrWhiteSpace(actual))
            return Result.Fail("Ingrese su contraseña actual.");
        if (string.IsNullOrWhiteSpace(nueva))
            return Result.Fail("Ingrese la nueva contraseña.");
        if (nueva.Length < 8)
            return Result.Fail("La nueva contraseña debe tener al menos 8 caracteres.");
        if (!nueva.Any(char.IsUpper))
            return Result.Fail("La nueva contraseña debe incluir al menos una letra mayúscula.");
        if (!nueva.Any(char.IsLower))
            return Result.Fail("La nueva contraseña debe incluir al menos una letra minúscula.");
        if (!nueva.Any(char.IsDigit))
            return Result.Fail("La nueva contraseña debe incluir al menos un número.");
        if (!nueva.Any(caracter => !char.IsLetterOrDigit(caracter)))
            return Result.Fail("La nueva contraseña debe incluir al menos un carácter especial.");
        if (string.IsNullOrWhiteSpace(confirmacion))
            return Result.Fail("Repita la nueva contraseña.");
        if (!string.Equals(nueva, confirmacion, StringComparison.Ordinal))
            return Result.Fail("La repetición no coincide con la nueva contraseña.");

        return Result.Ok();
    }
}
