namespace PuntoMarket.Gateway.Auth;

public static class AuthMessages
{
    public const string CredentialsRequired = "Ingrese su nombre de usuario y su contraseña.";
    public const string InvalidCredentials = "Usuario o contraseña incorrectos.";
    public const string UserNotEnabled = "Su usuario no está habilitado en el sistema.";
    public const string AuthenticationUnavailable = "El servicio de autenticación no está disponible. Intente nuevamente en unos minutos.";
    public const string UsersUnavailable = "El servicio de usuarios no está disponible. Intente nuevamente en unos minutos.";
}
