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
            errores.Add(new(CampoActual, MensajesPassword.ActualObligatoria));

        var errorNueva = ValidarNueva(nueva);
        if (errorNueva is not null)
            errores.Add(new(CampoNueva, errorNueva));

        if (string.IsNullOrWhiteSpace(confirmacion))
            errores.Add(new(CampoConfirmacion, MensajesPassword.RepeticionObligatoria));
        else if (errorNueva is null && !string.Equals(nueva, confirmacion, StringComparison.Ordinal))
            errores.Add(new(CampoConfirmacion, MensajesPassword.RepeticionDistinta));

        return errores;
    }

    public static Result Validar(string? actual, string? nueva, string? confirmacion)
    {
        var errores = ValidarCampos(actual, nueva, confirmacion);
        return errores.Count == 0 ? Result.Ok() : Result.Fail(errores[0].Mensaje);
    }

    public static string? ValidarNueva(string? nueva)
    {
        if (string.IsNullOrWhiteSpace(nueva))
            return MensajesPassword.NuevaObligatoria;
        if (nueva.Length < MensajesPassword.LongitudMinima)
            return MensajesPassword.NuevaCorta;
        if (!nueva.Any(char.IsUpper))
            return MensajesPassword.NuevaSinMayuscula;
        if (!nueva.Any(char.IsLower))
            return MensajesPassword.NuevaSinMinuscula;
        if (!nueva.Any(char.IsDigit))
            return MensajesPassword.NuevaSinNumero;
        if (!nueva.Any(caracter => !char.IsLetterOrDigit(caracter)))
            return MensajesPassword.NuevaSinEspecial;

        return null;
    }
}
