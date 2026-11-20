using Microsoft.Extensions.Options;
using PuntoMarket.Clientes.Application.Ports.Out;

namespace PuntoMarket.Clientes.Adapters.In.Api.Security;

/// <summary>
/// Implementa el puerto ICurrentUser leyendo el claim configurado en AuthSettings.UserIdClaim (por defecto "sub",
/// una cadena; no se asume un entero). Ese valor se guarda en idUsuarioCreacion / idUsuarioActualizacion.
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accesor, IOptions<AuthSettings> opciones) : ICurrentUser
{
    public string? UserId
    {
        get
        {
            var usuario = accesor.HttpContext?.User;
            if (usuario?.Identity?.IsAuthenticated != true) return null;
            var valor = usuario.FindFirst(opciones.Value.UserIdClaim)?.Value;
            return string.IsNullOrWhiteSpace(valor) ? null : valor;
        }
    }
}
