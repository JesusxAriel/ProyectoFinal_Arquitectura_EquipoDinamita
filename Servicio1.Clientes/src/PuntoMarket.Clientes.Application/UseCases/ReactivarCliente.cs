using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Application.Ports.In;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Errors;

namespace PuntoMarket.Clientes.Application.UseCases;

public sealed class ReactivarCliente(IClienteRepository repositorio, ICurrentUser usuarioActual, TimeProvider reloj) : IReactivarCliente
{
    public async Task<Result<ClienteDto>> EjecutarAsync(int id, CancellationToken cancellationToken)
    {
        var usuario = UsuarioHelper.ObtenerUsuario(usuarioActual);
        if (usuario.IsFailure) return Result.Failure<ClienteDto>(usuario.Error);

        var cliente = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (cliente is null) return Result.Failure<ClienteDto>(ClienteErrors.NoEncontrado(id));

        var reactivacion = cliente.Reactivar(usuario.Value, reloj.GetUtcNow().UtcDateTime);
        if (reactivacion.IsFailure) return Result.Failure<ClienteDto>(reactivacion.Error);

        // No se puede reactivar si otro cliente activo ya usa ese NIT/CI.
        if (await repositorio.ExisteActivoConNitCiAsync(cliente.NitCi, cliente.Id, cancellationToken))
            return Result.Failure<ClienteDto>(ClienteErrors.NitCiDuplicado);

        var guardado = await repositorio.ActualizarAsync(cliente, cancellationToken);
        return guardado.IsFailure
            ? Result.Failure<ClienteDto>(guardado.Error)
            : Result.Success(cliente.ToDto());
    }
}
