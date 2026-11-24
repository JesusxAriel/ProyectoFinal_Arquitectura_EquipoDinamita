using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Auth;

public interface IActivacionApiAdapter
{
    Task<Result> Activar(
        string token,
        string nuevaContrasena,
        string confirmarContrasena,
        CancellationToken cancellationToken = default);
}
