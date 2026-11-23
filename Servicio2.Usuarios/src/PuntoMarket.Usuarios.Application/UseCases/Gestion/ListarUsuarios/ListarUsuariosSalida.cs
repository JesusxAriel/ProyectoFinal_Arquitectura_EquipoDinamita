namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;

public sealed record ListarUsuariosSalida(IReadOnlyList<UsuarioSalida> Usuarios);
