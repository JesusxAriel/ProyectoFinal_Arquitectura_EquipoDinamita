namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.CrearUsuario;

public sealed record CrearUsuarioEntrada(string? Nombres, string? ApellidoPaterno, string? ApellidoMaterno, string? Correo, int RolId);
