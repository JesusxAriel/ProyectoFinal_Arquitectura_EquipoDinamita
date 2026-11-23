using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ActualizarUsuario;

public sealed class ActualizarUsuarioCasoDeUso(
    IUsuarioRepositorio repositorio,
    IUsuarioActual usuarioActual,
    IReloj reloj)
{
    public async Task<Result<ActualizarUsuarioSalida>> EjecutarAsync(
        ActualizarUsuarioEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var rol = await repositorio.ObtenerRol(entrada.RolId, cancellationToken);
        if (rol is null || !rol.Activo)
            return Result<ActualizarUsuarioSalida>.Fail(MensajesDeGestion.RolNoDisponible);

        var usuario = await repositorio.ObtenerPorId(entrada.Id, cancellationToken);
        if (usuario is null)
            return Result<ActualizarUsuarioSalida>.Fail(MensajesDeGestion.UsuarioNoEncontrado);

        var cambio = usuario.Actualizar(
            entrada.Nombres,
            entrada.Apellidos,
            entrada.Correo,
            entrada.RolId,
            usuarioActual.Username,
            reloj.Ahora);

        if (cambio.IsFail)
            return Result<ActualizarUsuarioSalida>.Fail(cambio.Error);

        var guardado = await repositorio.Actualizar(usuario, cancellationToken);
        if (guardado.IsFail)
            return Result<ActualizarUsuarioSalida>.Fail(guardado.Error);

        return Result<ActualizarUsuarioSalida>.Ok(
            new ActualizarUsuarioSalida(UsuarioSalida.Desde(usuario, rol.Nombre)));
    }
}
