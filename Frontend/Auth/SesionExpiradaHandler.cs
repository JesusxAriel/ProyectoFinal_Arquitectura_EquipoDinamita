namespace PuntoMarket.Frontend.Auth;

public sealed class SesionExpiradaHandler(ISesionUsuario sesion) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var respuesta = await base.SendAsync(request, cancellationToken);

        if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized && EraLlamadaAutenticada(request))
        {
            sesion.Cerrar();
        }

        return respuesta;
    }

    private static bool EraLlamadaAutenticada(HttpRequestMessage request) =>
        request.Headers.Authorization is not null
        && request.RequestUri?.AbsolutePath.EndsWith(AuthApiAdapter.LoginRoute, StringComparison.OrdinalIgnoreCase) != true;
}
