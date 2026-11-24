using PuntoMarket.Usuarios.Domain.Entities;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion;

public sealed record UsuarioSalida(
    int Id,
    string Username,
    string Nombres,
    string ApellidoPaterno,
    string? ApellidoMaterno,
    string NombreCompleto,
    string Correo,
    string Rol,
    bool Activo,
    bool DebeCambiarPassword)
{
    public static UsuarioSalida Desde(Usuario usuario, string rol) =>
        new(usuario.Id,
            usuario.Username,
            usuario.Nombres,
            usuario.ApellidoPaterno,
            usuario.ApellidoMaterno,
            usuario.NombreCompleto,
            usuario.Correo,
            rol,
            usuario.Activo,
            usuario.DebeCambiarPassword);
}
