using PuntoMarket.Clientes.Application.Ports.In;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Errors;

namespace PuntoMarket.Clientes.Application.UseCases;

/// <summary>Baja lógica: estaActivo = false. No se borra la fila.</summary>
public sealed class DesactivarCliente(IClienteRepository repositorio, ICurrentUser usuarioActual, TimeProvider reloj) : IDesactivarCliente
{
    public async Task<Result> EjecutarAsync(int id, CancellationToken cancellationToken)
    {
        var usuario = UsuarioHelper.ObtenerUsuario(usuarioActual);
        if (usuario.IsFailure) return Result.Failure(usuario.Error);

        var cliente = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (cliente is null) return Result.Failure(ClienteErrors.NoEncontrado(id));

        var baja = cliente.DarDeBaja(usuario.Value, reloj.GetUtcNow().UtcDateTime);
        if (baja.IsFailure) return baja;

        return await repositorio.ActualizarAsync(cliente, cancellationToken);
    }
}
