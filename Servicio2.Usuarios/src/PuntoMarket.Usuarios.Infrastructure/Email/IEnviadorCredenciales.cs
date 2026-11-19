using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Infrastructure.Email;

public interface IEnviadorCredenciales
{
    Task<Result> EnviarCredenciales(
        string correo,
        string username,
        string passwordTemporal,
        CancellationToken cancellationToken = default);
}
