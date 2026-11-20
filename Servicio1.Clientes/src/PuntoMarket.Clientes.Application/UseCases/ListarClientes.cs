using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Application.Ports.In;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Common;

namespace PuntoMarket.Clientes.Application.UseCases;

public sealed class ListarClientes(IClienteRepository repositorio) : IListarClientes
{
    public async Task<Result<PaginaResultado<ClienteDto>>> EjecutarAsync(
        ListarClientesQuery consulta, CancellationToken cancellationToken)
    {
        var valida = consulta.Validar();
        if (valida.IsFailure) return Result.Failure<PaginaResultado<ClienteDto>>(valida.Error);

        var normalizada = consulta.Normalizada();
        var pagina = await repositorio.ListarAsync(normalizada, cancellationToken);

        var items = pagina.Items.Select(c => c.ToDto()).ToList();
        return Result.Success(new PaginaResultado<ClienteDto>(items, normalizada.Pagina, normalizada.TamanoPagina, pagina.Total));
    }
}
