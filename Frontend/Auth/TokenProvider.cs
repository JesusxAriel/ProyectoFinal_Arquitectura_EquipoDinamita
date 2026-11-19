namespace PuntoMarket.Frontend.Auth;

public interface ITokenProvider
{
    string? Obtener(ISession sesion);
    void Guardar(ISession sesion, string token);
}

public sealed class TokenProvider : ITokenProvider
{
    private const string TokenKey = "auth.token";

    private static readonly Lazy<TokenProvider> LazyInstance = new(() => new TokenProvider());

    private TokenProvider()
    {
    }

    public static TokenProvider Instance => LazyInstance.Value;

    public string? Obtener(ISession sesion) => sesion.GetString(TokenKey);

    public void Guardar(ISession sesion, string token) => sesion.SetString(TokenKey, token);
}
