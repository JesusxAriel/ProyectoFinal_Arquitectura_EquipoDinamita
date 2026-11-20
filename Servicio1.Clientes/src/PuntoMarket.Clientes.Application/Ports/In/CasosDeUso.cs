using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Common;

namespace PuntoMarket.Clientes.Application.Ports.In;

public sealed record ListarClientesQuery(string? Busqueda, string Estado = "Todos", string OrdenarPor = "RazonSocial",
    bool Descendente = false, int Pagina = 1, int TamanoPagina = 10);

public interface IListarClientes { Task<Result<PaginaResultado<ClienteDto>>> EjecutarAsync(ListarClientesQuery query, CancellationToken ct); }
public interface IObtenerCliente { Task<Result<ClienteDto>> EjecutarAsync(int id, CancellationToken ct); }
public interface ICrearCliente { Task<Result<ClienteDto>> EjecutarAsync(GuardarClienteRequest request, CancellationToken ct); }
public interface IActualizarCliente { Task<Result<ClienteDto>> EjecutarAsync(int id, GuardarClienteRequest request, CancellationToken ct); }
public interface IDesactivarCliente { Task<Result> EjecutarAsync(int id, CancellationToken ct); }
public interface IReactivarCliente { Task<Result<ClienteDto>> EjecutarAsync(int id, CancellationToken ct); }
