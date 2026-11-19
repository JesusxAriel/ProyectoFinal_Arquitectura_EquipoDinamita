using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Auth;

public static class ValidadorCredenciales
{
    public static Result Validar(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Fail(MensajesAuth.UsuarioRequerido);
        }

        return string.IsNullOrEmpty(password)
            ? Result.Fail(MensajesAuth.PasswordRequerida)
            : Result.Ok();
    }
}
