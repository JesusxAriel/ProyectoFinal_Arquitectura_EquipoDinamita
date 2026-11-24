using PuntoMarket.Usuarios.Domain;
using PuntoMarket.Usuarios.Domain.Entities;

namespace PuntoMarket.Usuarios.Application.Ports.Gestion;

public sealed record TokenConsumido(Usuario Usuario, DateTime ExpiraUtc);

public interface IActivacionRepositorio
{
    Task<Result> GuardarToken(
        int usuarioId,
        string hash,
        DateTime expiraUtc,
        CancellationToken cancellationToken = default);

    Task<TokenConsumido?> ConsumirToken(
        string hash,
        DateTime ahoraUtc,
        CancellationToken cancellationToken = default);

    Task RestaurarToken(
        int usuarioId,
        string hash,
        DateTime expiraUtc,
        CancellationToken cancellationToken = default);
}
