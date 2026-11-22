namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.CrearUsuario;

public sealed record CrearUsuarioEntrada(string? Nombres, string? Apellidos, string? Correo, int RolId);
