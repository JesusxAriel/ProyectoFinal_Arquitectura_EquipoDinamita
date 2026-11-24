using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Application.Ports.Gestion;

public interface IGestorDeIdentidad
{
    Task<Result> CrearUsuario(
        string username,
        string correo,
        string nombres,
        string apellidos,
        string rol,
        string passwordTemporal);

    Task<Result> CambiarPassword(string username, string passwordNueva);

    Task<Result> VerificarPassword(string username, string passwordActual);

    Task<Result> CambiarEstado(string username, bool activo);
}
