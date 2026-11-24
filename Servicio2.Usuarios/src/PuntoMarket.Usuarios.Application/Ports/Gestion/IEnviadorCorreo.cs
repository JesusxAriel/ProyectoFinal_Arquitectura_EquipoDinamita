using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Application.Ports.Gestion;

public interface IEnviadorCorreo
{
    Task<Result> EnviarCredenciales(
        string correo,
        string username,
        string passwordTemporal,
        CancellationToken cancellationToken = default);
}
