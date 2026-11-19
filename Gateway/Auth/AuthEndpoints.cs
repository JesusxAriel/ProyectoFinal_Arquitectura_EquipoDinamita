using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Gateway.Security;

namespace PuntoMarket.Gateway.Auth;

public static class AuthEndpoints
{
    private const string LoginRoute = "/api/auth/login";
    private const string LogoutRoute = "/api/auth/logout";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost(LoginRoute, Login).AllowAnonymous();
        routes.MapPost(LogoutRoute, Logout).RequireAuthorization(GatewayPolicies.TokenValido);
        return routes;
    }

    private static async Task<IResult> Login(
        LoginRequest request, LoginService service, CancellationToken cancellationToken)
    {
        var result = await service.Login(request, cancellationToken);
        return result.Status switch
        {
            LoginStatus.Success => Results.Ok(result.Response),
            LoginStatus.InvalidRequest => ErrorResponse(StatusCodes.Status400BadRequest, result.Error),
            LoginStatus.InvalidCredentials => ErrorResponse(StatusCodes.Status401Unauthorized, result.Error),
            _ => ErrorResponse(StatusCodes.Status503ServiceUnavailable, result.Error)
        };
    }

    private static async Task<IResult> Logout(
        [FromBody] LogoutRequest? request, KeycloakClient keycloak, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            await keycloak.RevokeRefreshToken(request.RefreshToken, cancellationToken);
        }

        return Results.NoContent();
    }

    private static IResult ErrorResponse(int statusCode, string error) =>
        Results.Json(new { error }, statusCode: statusCode);
}
