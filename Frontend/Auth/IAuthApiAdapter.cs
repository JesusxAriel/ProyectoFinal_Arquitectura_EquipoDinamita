using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Auth;

public interface IAuthApiAdapter
{
    Task<Result<LoginResponse>> Login(LoginRequest request);
    Task<Result> Logout();
}
