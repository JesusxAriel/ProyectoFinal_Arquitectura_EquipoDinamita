namespace PuntoMarket.Gateway.Auth;

public enum LoginStatus
{
    Success,
    InvalidRequest,
    InvalidCredentials,
    Unavailable
}

public sealed record LoginResult(LoginStatus Status, LoginResponse? Response, string Error)
{
    public static LoginResult Success(LoginResponse response) => new(LoginStatus.Success, response, "");
    public static LoginResult Failure(LoginStatus status, string error) => new(status, null, error);
}

public sealed class LoginService(KeycloakClient keycloak, UsuariosClient usuarios)
{
    public async Task<LoginResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return LoginResult.Failure(LoginStatus.InvalidRequest, AuthMessages.CredentialsRequired);
        }

        var token = await keycloak.RequestToken(request.Username, request.Password, cancellationToken);
        if (token.Status != ClientStatus.Ok)
        {
            return FailureOf(token.Status, AuthMessages.InvalidCredentials, AuthMessages.AuthenticationUnavailable);
        }

        var user = await usuarios.GetCurrentUser(token.Value!.AccessToken, cancellationToken);
        if (user.Status != ClientStatus.Ok)
        {
            return FailureOf(user.Status, AuthMessages.UserNotEnabled, AuthMessages.UsersUnavailable);
        }

        return user.Value!.Activo
            ? LoginResult.Success(BuildResponse(token.Value, user.Value))
            : LoginResult.Failure(LoginStatus.InvalidCredentials, AuthMessages.UserNotEnabled);
    }

    private static LoginResult FailureOf(ClientStatus status, string rejectedMessage, string unavailableMessage) =>
        status == ClientStatus.Rejected
            ? LoginResult.Failure(LoginStatus.InvalidCredentials, rejectedMessage)
            : LoginResult.Failure(LoginStatus.Unavailable, unavailableMessage);

    private static LoginResponse BuildResponse(KeycloakToken token, UsuarioActual user) =>
        new(token.AccessToken, token.ExpiresIn, user.Username, user.NombreCompleto, user.Rol, user.DebeCambiarPassword);
}
