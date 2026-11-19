using Proveedores.Application.Abstractions;
using Proveedores.Application.Dtos;
using Proveedores.Domain.Common;

namespace Proveedores.Application.UseCases;

public interface IListarProveedores
{
    Task<Result<PaginaResultado<ProveedorDto>>> EjecutarAsync(ListarProveedoresQuery consulta, CancellationToken cancellationToken);
}

public sealed class ListarProveedores(IProveedorRepository repositorio) : IListarProveedores
{
    public async Task<Result<PaginaResultado<ProveedorDto>>> EjecutarAsync(ListarProveedoresQuery consulta, CancellationToken cancellationToken)
    {
        var validacion = consulta.Validar();
        if (validacion.IsFailure) return Result.Failure<PaginaResultado<ProveedorDto>>(validacion.Error);
        var q = consulta.Normalizada();
        var pagina = await repositorio.ListarAsync(q, cancellationToken);
        var items = pagina.Items.Select(p => p.ToDto()).ToList();
        return Result.Success(new PaginaResultado<ProveedorDto>(items, q.Pagina, q.TamanoPagina, pagina.Total));
    }
}
