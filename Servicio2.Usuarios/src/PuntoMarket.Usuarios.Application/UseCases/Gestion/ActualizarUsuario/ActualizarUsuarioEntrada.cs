namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ActualizarUsuario;

public sealed record ActualizarUsuarioEntrada(
    int Id,
    string? Nombres,
    string? ApellidoPaterno,
    string? ApellidoMaterno,
    string? Correo,
    int RolId);
