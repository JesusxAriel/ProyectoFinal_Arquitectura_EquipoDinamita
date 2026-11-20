using System.Security.Claims;
using PuntoMarket.Clientes.Application.Ports.Out;

namespace PuntoMarket.Clientes.Adapters.In.Api.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? Id => accessor.HttpContext?.User.FindFirst("sub")?.Value
        ?? accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
