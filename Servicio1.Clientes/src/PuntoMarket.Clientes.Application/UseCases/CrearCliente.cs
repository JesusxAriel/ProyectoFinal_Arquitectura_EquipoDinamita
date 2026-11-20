using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Application.Ports.In;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Creators;
using PuntoMarket.Clientes.Domain.Entities;
using PuntoMarket.Clientes.Domain.Errors;

namespace PuntoMarket.Clientes.Application.UseCases;

public sealed class CrearCliente(IClienteRepository repositorio, ICurrentUser usuarioActual, TimeProvider reloj) : ICrearCliente
{
    public async Task<Result<ClienteDto>> EjecutarAsync(GuardarClienteRequest request, CancellationToken cancellationToken)
    {
        // La auditoría procede del puerto ICurrentUser (claim validado por la API), nunca del formulario.
        var usuario = UsuarioHelper.ObtenerUsuario(usuarioActual);
        if (usuario.IsFailure) return Result.Failure<ClienteDto>(usuario.Error);

        var datos = new DatosCliente(request.NitCi ?? string.Empty, request.RazonSocial ?? string.Empty, request.Correo, request.Telefono);

        // FACTORY METHOD: el creador concreto valida y construye el cliente.
        var creacion = ClienteCreatorSelector.Para(TipoCliente.Estandar)
            .Crear(datos, usuario.Value, reloj.GetUtcNow().UtcDateTime);
        if (creacion.IsFailure) return Result.Failure<ClienteDto>(creacion.Error);

        var cliente = creacion.Value;

        // Comprobación amistosa previa; la garantía real contra concurrencia es el índice único de la base de datos.
        if (await repositorio.ExisteActivoConNitCiAsync(cliente.NitCi, null, cancellationToken))
            return Result.Failure<ClienteDto>(ClienteErrors.NitCiDuplicado);

        var guardado = await repositorio.AgregarAsync(cliente, cancellationToken);
        return guardado.IsFailure
            ? Result.Failure<ClienteDto>(guardado.Error)
            : Result.Success(guardado.Value.ToDto());
    }
}
