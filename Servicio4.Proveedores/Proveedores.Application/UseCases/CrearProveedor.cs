using Proveedores.Application.Abstractions;
using Proveedores.Application.Dtos;
using Proveedores.Domain.Common;
using Proveedores.Domain.Creators;
using Proveedores.Domain.Entities;
using Proveedores.Domain.Errors;

namespace Proveedores.Application.UseCases;

public interface ICrearProveedor
{
    Task<Result<ProveedorDto>> EjecutarAsync(GuardarProveedorRequest request, CancellationToken cancellationToken);
}

public sealed class CrearProveedor(IProveedorRepository repositorio, ICurrentUser usuarioActual, TimeProvider reloj) : ICrearProveedor
{
    public async Task<Result<ProveedorDto>> EjecutarAsync(GuardarProveedorRequest request, CancellationToken cancellationToken)
    {
        // INTEGRACIÓN: La auditoría procede del puerto ICurrentUser (claim validado por la API),
        //   no del formulario ni de una consulta a la base de Usuarios. El reloj también se inyecta.
        var usuario = UsuarioHelper.ObtenerUsuario(usuarioActual);
        if (usuario.IsFailure) return Result.Failure<ProveedorDto>(usuario.Error);

        var datos = new DatosProveedor(request.NombreEmpresa, request.NumeroEmpresa, request.CorreoReferencia, request.EsAutogestionado);

        // FACTORY METHOD: el creador concreto valida y construye el proveedor.
        var creacion = ProveedorCreatorSelector.Para(request.EsAutogestionado)
            .Crear(datos, usuario.Value, reloj.GetUtcNow().UtcDateTime);
        if (creacion.IsFailure) return Result.Failure<ProveedorDto>(creacion.Error);

        var proveedor = creacion.Value;

        // Comprobación amistosa previa; la garantía real contra concurrencia es el índice único parcial de MongoDB.
        if (await repositorio.ExisteActivoConNombreAsync(proveedor.NombreNormalizado, null, cancellationToken))
            return Result.Failure<ProveedorDto>(ProveedorErrors.NombreDuplicado);

        var guardado = await repositorio.AgregarAsync(proveedor, cancellationToken);
        return guardado.IsFailure
            ? Result.Failure<ProveedorDto>(guardado.Error)
            : Result.Success(guardado.Value.ToDto());
    }
}
