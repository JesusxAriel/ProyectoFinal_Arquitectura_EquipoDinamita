using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Application.Ports.In;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Entities;
using PuntoMarket.Clientes.Domain.Errors;

namespace PuntoMarket.Clientes.Application.UseCases;

public sealed class ActualizarCliente(IClienteRepository repositorio, ICurrentUser usuarioActual, TimeProvider reloj) : IActualizarCliente
{
    public async Task<Result<ClienteDto>> EjecutarAsync(int id, GuardarClienteRequest request, CancellationToken cancellationToken)
    {
        var usuario = UsuarioHelper.ObtenerUsuario(usuarioActual);
        if (usuario.IsFailure) return Result.Failure<ClienteDto>(usuario.Error);

        var cliente = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (cliente is null) return Result.Failure<ClienteDto>(ClienteErrors.NoEncontrado(id));

        var datos = new DatosCliente(request.NitCi ?? string.Empty, request.RazonSocial ?? string.Empty, request.Correo, request.Telefono);

        // La entidad aplica sus reglas (genérico protegido, inactivo, validación) y devuelve Result.
        var cambio = cliente.Actualizar(datos, usuario.Value, reloj.GetUtcNow().UtcDateTime);
        if (cambio.IsFailure) return Result.Failure<ClienteDto>(cambio.Error);

        if (await repositorio.ExisteActivoConNitCiAsync(cliente.NitCi, cliente.Id, cancellationToken))
            return Result.Failure<ClienteDto>(ClienteErrors.NitCiDuplicado);

        var guardado = await repositorio.ActualizarAsync(cliente, cancellationToken);
        return guardado.IsFailure
            ? Result.Failure<ClienteDto>(guardado.Error)
            : Result.Success(cliente.ToDto());
    }
}
