using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Auth;

public interface IPerfilApiAdapter
{
    Task<Result> CambiarPassword(CambiarPasswordRequest request);

    Task<Result<bool>> VerificarPasswordActual(string actual);
}
