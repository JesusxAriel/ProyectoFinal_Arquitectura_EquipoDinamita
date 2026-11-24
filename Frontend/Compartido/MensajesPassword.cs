namespace PuntoMarket.Frontend.Compartido;

public static class MensajesPassword
{
    public const int LongitudMinima = 8;

    public const string ActualObligatoria = "Ingrese su contraseña actual.";
    public const string NuevaObligatoria = "Ingrese la nueva contraseña.";
    public const string NuevaCorta = "La nueva contraseña debe tener al menos 8 caracteres.";
    public const string NuevaSinMayuscula = "La nueva contraseña debe incluir al menos una letra mayúscula.";
    public const string NuevaSinMinuscula = "La nueva contraseña debe incluir al menos una letra minúscula.";
    public const string NuevaSinNumero = "La nueva contraseña debe incluir al menos un número.";
    public const string NuevaSinEspecial = "La nueva contraseña debe incluir al menos un carácter especial.";
    public const string RepeticionObligatoria = "Repita la nueva contraseña.";
    public const string Verificando = "Verificando…";
    public const string ActualCorrecta = "Contraseña actual correcta.";
    public const string RepeticionDistinta = "La repetición no coincide con la nueva contraseña.";
}
