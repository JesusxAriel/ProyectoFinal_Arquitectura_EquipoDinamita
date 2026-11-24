using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Application.Ports.Gestion;

public interface IEnviadorCorreo
{
    Task<Result> EnviarActivacion(
        string correo,
        string username,
        string token,
        TimeSpan vigencia,
        CancellationToken cancellationToken = default);
}
