using Microsoft.Extensions.Options;
using Proveedores.Application.Abstractions;

namespace Proveedores.API.Security;

// =====================================================
// INTEGRACIÓN GRUPO 2 - KEYCLOAK  (claim "sub" para auditoría)
// ESTADO: PREPARADO
// QUÉ HACE: implementa ICurrentUser leyendo el claim configurado en AuthSettings.UserIdClaim
//   (por defecto "sub", una cadena; no se asume un entero). Ese valor se guarda en
//   idUsuarioAdmin / idUsuarioActualizacion.
// QUÉ DEBE MODIFICAR EL GRUPO 2: solo UserIdClaim si deciden auditar con otro claim.
// =====================================================
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
