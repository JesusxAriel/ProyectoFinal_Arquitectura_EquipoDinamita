namespace PuntoMarket.Frontend.Compartido;

public sealed record ErrorDeCampo(string Campo, string Mensaje);

public static class ValidadorPassword
{
    public const string CampoActual = "PasswordActual";
    public const string CampoNueva = "PasswordNueva";
    public const string CampoConfirmacion = "RepetirPasswordNueva";

    public static IReadOnlyList<ErrorDeCampo> ValidarCampos(string? actual, string? nueva, string? confirmacion)
    {
        var errores = new List<ErrorDeCampo>();

        if (string.IsNullOrWhiteSpace(actual))
            errores.Add(new(CampoActual, "Ingrese su contraseña actual."));

        var errorNueva = ValidarNueva(nueva);
        if (errorNueva is not null)
            errores.Add(new(CampoNueva, errorNueva));

        if (string.IsNullOrWhiteSpace(confirmacion))
            errores.Add(new(CampoConfirmacion, "Repita la nueva contraseña."));
        else if (errorNueva is null && !string.Equals(nueva, confirmacion, StringComparison.Ordinal))
            errores.Add(new(CampoConfirmacion, "La repetición no coincide con la nueva contraseña."));

        return errores;
    }

    public static Result Validar(string? actual, string? nueva, string? confirmacion)
    {
        var errores = ValidarCampos(actual, nueva, confirmacion);
        return errores.Count == 0 ? Result.Ok() : Result.Fail(errores[0].Mensaje);
    }

    private static string? ValidarNueva(string? nueva)
    {
        if (string.IsNullOrWhiteSpace(nueva))
            return "Ingrese la nueva contraseña.";
        if (nueva.Length < 8)
            return "La nueva contraseña debe tener al menos 8 caracteres.";
        if (!nueva.Any(char.IsUpper))
            return "La nueva contraseña debe incluir al menos una letra mayúscula.";
        if (!nueva.Any(char.IsLower))
            return "La nueva contraseña debe incluir al menos una letra minúscula.";
        if (!nueva.Any(char.IsDigit))
            return "La nueva contraseña debe incluir al menos un número.";
        if (!nueva.Any(caracter => !char.IsLetterOrDigit(caracter)))
            return "La nueva contraseña debe incluir al menos un carácter especial.";

        return null;
    }
}
