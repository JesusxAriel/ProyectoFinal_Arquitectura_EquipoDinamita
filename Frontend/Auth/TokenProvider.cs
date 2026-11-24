namespace PuntoMarket.Frontend.Auth;

public interface ITokenProvider
{
    string? Obtener(ISession sesion);
    void Guardar(ISession sesion, string token);
}

public sealed class TokenProvider : ITokenProvider
{
    private const string TokenKey = "auth.token";

    private static volatile TokenProvider? _instance;
    private static readonly object _bloqueo = new();

    private TokenProvider()
    {
    }

    public static TokenProvider Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (_bloqueo)
                {
                    if (_instance is null)
                    {
                        _instance = new TokenProvider();
                    }
                }
            }

            return _instance;
        }
    }

    public string? Obtener(ISession sesion) => sesion.GetString(TokenKey);

    public void Guardar(ISession sesion, string token) => sesion.SetString(TokenKey, token);
}
