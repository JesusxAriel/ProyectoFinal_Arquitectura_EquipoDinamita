using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Entities;

namespace PuntoMarket.Clientes.Application.Ports.Out;

public sealed record ConsultaClientes(string? Busqueda, string Estado, string OrdenarPor, bool Descendente, int Pagina, int TamanoPagina);
public interface IClienteRepository
{
    Task<Cliente?> ObtenerAsync(int id, CancellationToken ct);
    Task<(IReadOnlyList<Cliente> Items, long Total)> ListarAsync(ConsultaClientes consulta, CancellationToken ct);
    Task<Result<Cliente>> CrearAsync(Cliente cliente, CancellationToken ct);
    Task<Result<Cliente>> ActualizarAsync(Cliente cliente, CancellationToken ct);
    Task<Result> DesactivarAsync(Cliente cliente, string usuario, DateTime fecha, CancellationToken ct);
    Task<Result<Cliente>> ReactivarAsync(Cliente cliente, string usuario, DateTime fecha, CancellationToken ct);
    Task<bool> ExisteNitCiActivoAsync(string nitCi, int? excluirId, CancellationToken ct);
}

public interface ICurrentUser
{
    string? Id { get; }
}
