namespace PuntoMarket.Frontend.Auth;

public static class MensajesActivacion
{
    public const string EnlaceInvalido =
        "El enlace de activación no es válido o ha vencido. Solicite uno nuevo al administrador.";

    public const string ReferenciaExpirada = "Vuelva a abrir el enlace que recibió por correo.";

    public const string PasswordRequerida = "Ingrese la nueva contraseña.";

    public const string ConfirmacionRequerida = "Confirme la nueva contraseña.";

    public const string ActivacionExitosa = "Su cuenta fue activada. Ya puede iniciar sesión.";
}
