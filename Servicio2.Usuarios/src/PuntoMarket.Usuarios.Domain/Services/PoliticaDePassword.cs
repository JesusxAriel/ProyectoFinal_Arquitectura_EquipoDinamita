namespace PuntoMarket.Usuarios.Domain.Services;

public static class PoliticaDePassword
{
    public const int LongitudMinima = 8;

    public static Result Validar(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return Result.Fail("La contraseña es obligatoria.");

        if (password.Length < LongitudMinima)
            return Result.Fail($"La contraseña debe tener al menos {LongitudMinima} caracteres.");

        if (!password.Any(char.IsUpper))
            return Result.Fail("La contraseña debe tener al menos una letra mayúscula.");

        if (!password.Any(char.IsLower))
            return Result.Fail("La contraseña debe tener al menos una letra minúscula.");

        if (!password.Any(char.IsDigit))
            return Result.Fail("La contraseña debe tener al menos un número.");

        if (!password.Any(caracter => !char.IsLetterOrDigit(caracter)))
            return Result.Fail("La contraseña debe tener al menos un carácter especial.");

        return Result.Ok();
    }

    public static Result Coinciden(string? nueva, string? confirmacion) =>
        string.Equals(nueva, confirmacion, StringComparison.Ordinal)
            ? Result.Ok()
            : Result.Fail("La confirmación no coincide con la contraseña nueva.");
}
