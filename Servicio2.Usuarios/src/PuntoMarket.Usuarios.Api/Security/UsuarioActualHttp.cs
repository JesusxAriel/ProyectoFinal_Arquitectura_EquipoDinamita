using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PuntoMarket.Usuarios.Application.Ports.Comunes;

namespace PuntoMarket.Usuarios.Api.Security;

public interface IUsernameActual
{
    string Username { get; }
}

public sealed class UsuarioActualHttp : IUsuarioActual, IUsernameActual
{
    private const string UsernameClaim = "preferred_username";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioActualHttp(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    public int Id
    {
        get
        {
            var claim = ObtenerIdentidadAutenticada().FindFirst("sub");
            if (claim is null ||
                !int.TryParse(claim.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ||
                id <= 0)
            {
                throw new InvalidOperationException(
                    "El usuario autenticado no tiene un claim 'sub' válido.");
            }

            return id;
        }
    }

    public string Rol
    {
        get
        {
            var claim = ObtenerIdentidadAutenticada().FindFirst(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(claim?.Value))
            {
                throw new InvalidOperationException(
                    "El usuario autenticado no tiene un rol válido.");
            }

            return claim.Value;
        }
    }

    public string Username
    {
        get
        {
            var claim = ObtenerIdentidadAutenticada().FindFirst(UsernameClaim);
            if (string.IsNullOrWhiteSpace(claim?.Value))
            {
                throw new InvalidOperationException(
                    "El usuario autenticado no tiene el claim 'preferred_username'.");
            }

            return claim.Value;
        }
    }

    private ClaimsIdentity ObtenerIdentidadAutenticada()
    {
        var usuario = _httpContextAccessor.HttpContext?.User;
        var identidad = usuario?.Identities.FirstOrDefault(identity => identity.IsAuthenticated);

        return identidad ?? throw new InvalidOperationException(
            "No hay un usuario autenticado en el contexto HTTP actual.");
    }
}
