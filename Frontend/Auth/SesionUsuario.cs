using System.Text.Json;

namespace PuntoMarket.Frontend.Auth;

public interface ISesionUsuario
{
    DatosSesion? Actual { get; }
    void Iniciar(LoginResponse login);
    void Cerrar();
}

public sealed class SesionUsuario(
    IHttpContextAccessor accessor, ITokenProvider tokenProvider, TimeProvider reloj) : ISesionUsuario
{
    private const string DatosKey = "auth.datos";

    private ISession Sesion => accessor.HttpContext!.Session;

    public DatosSesion? Actual
    {
        get
        {
            var json = Sesion.GetString(DatosKey);
            var datos = json is null ? null : JsonSerializer.Deserialize<DatosSesion>(json);
            return datos is not null && datos.ExpiraEn > reloj.GetUtcNow() ? datos : null;
        }
    }

    public void Iniciar(LoginResponse login)
    {
        Sesion.Clear();
        tokenProvider.Guardar(Sesion, login.AccessToken);
        var datos = new DatosSesion(
            login.Username,
            login.NombreCompleto,
            login.Rol,
            login.DebeCambiarPassword,
            reloj.GetUtcNow().AddSeconds(login.ExpiresIn));
        Sesion.SetString(DatosKey, JsonSerializer.Serialize(datos));
    }

    public void Cerrar() => Sesion.Clear();
}
