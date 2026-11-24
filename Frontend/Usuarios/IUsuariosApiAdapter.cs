using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Usuarios;

public interface IUsuariosApiAdapter
{
    Task<Result<IReadOnlyList<UsuarioResponse>>> Listar();
    Task<Result<UsuarioResponse>> Obtener(int id);
    Task<Result<IReadOnlyList<RolResponse>>> ListarRoles();
    Task<Result<CreateUsuarioResponse>> Crear(CreateUsuarioRequest request);
    Task<Result> Actualizar(int id, UpdateUsuarioRequest request);
    Task<Result> CambiarEstado(int id, ChangeStatusRequest request);
    Task<Result<ReenviarAccesoResponse>> Reenviar(int id);
}