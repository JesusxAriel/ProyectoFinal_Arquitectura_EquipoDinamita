using System.Net.Http.Headers;

namespace PuntoMarket.Frontend.Auth;

public sealed class BearerTokenHandler(IHttpContextAccessor accessor, ITokenProvider tokenProvider)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sesion = accessor.HttpContext?.Session;
        var token = sesion is null ? null : tokenProvider.Obtener(sesion);

        if (!string.IsNullOrEmpty(token) && request.Headers.Authorization is null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
