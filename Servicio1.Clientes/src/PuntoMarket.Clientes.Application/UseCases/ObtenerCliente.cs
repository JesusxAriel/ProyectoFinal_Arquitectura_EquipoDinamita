using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Application.Ports.In;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Errors;

namespace PuntoMarket.Clientes.Application.UseCases;

public sealed class ObtenerCliente(IClienteRepository repositorio) : IObtenerCliente
{
    public async Task<Result<ClienteDto>> EjecutarAsync(int id, CancellationToken cancellationToken)
    {
        var cliente = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        return cliente is null
            ? Result.Failure<ClienteDto>(ClienteErrors.NoEncontrado(id))
            : Result.Success(cliente.ToDto());
    }
}
