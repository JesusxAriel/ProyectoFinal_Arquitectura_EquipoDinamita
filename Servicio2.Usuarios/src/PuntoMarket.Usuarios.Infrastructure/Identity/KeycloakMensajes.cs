using PuntoMarket.Usuarios.Application.UseCases.Gestion;

namespace PuntoMarket.Usuarios.Infrastructure.Identity;

public static class KeycloakMensajes
{
    public const string ServicioNoDisponible = "El servicio de identidad no está disponible. Intente nuevamente en unos minutos.";
    public const string UsuarioExistente = "El nombre de usuario ya existe en el sistema de identidad.";
    public const string UsuarioNoEncontrado = "El usuario no existe en el sistema de identidad.";
    public const string RolNoEncontrado = "El rol indicado no existe en el sistema de identidad.";
    public const string PasswordNoCumplePolitica = "La contraseña no cumple la política de seguridad.";
    public const string PasswordActualIncorrecta = MensajesDeGestion.PasswordActualIncorrecta;
    public const string OperacionRechazada = "El sistema de identidad rechazó la operación.";
}
