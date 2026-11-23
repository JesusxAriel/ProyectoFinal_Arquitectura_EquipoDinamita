namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ActualizarUsuario;

public sealed record ActualizarUsuarioEntrada(
    int Id,
    string? Nombres,
    string? Apellidos,
    string? Correo,
    int RolId);
