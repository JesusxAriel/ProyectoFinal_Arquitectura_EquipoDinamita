using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Domain.Common;

namespace PuntoMarket.Clientes.Application.Ports.In;

// PUERTOS DE ENTRADA (arquitectura hexagonal): lo que el exterior (la API HTTP) puede pedirle a la aplicación.
// Cada uno lo implementa un caso de uso de Application/UseCases.

public interface IListarClientes
{
    Task<Result<PaginaResultado<ClienteDto>>> EjecutarAsync(ListarClientesQuery consulta, CancellationToken cancellationToken);
}

public interface IObtenerCliente
{
    Task<Result<ClienteDto>> EjecutarAsync(int id, CancellationToken cancellationToken);
}

public interface ICrearCliente
{
    Task<Result<ClienteDto>> EjecutarAsync(GuardarClienteRequest request, CancellationToken cancellationToken);
}

public interface IActualizarCliente
{
    Task<Result<ClienteDto>> EjecutarAsync(int id, GuardarClienteRequest request, CancellationToken cancellationToken);
}

public interface IDesactivarCliente
{
    Task<Result> EjecutarAsync(int id, CancellationToken cancellationToken);
}

public interface IReactivarCliente
{
    Task<Result<ClienteDto>> EjecutarAsync(int id, CancellationToken cancellationToken);
}
