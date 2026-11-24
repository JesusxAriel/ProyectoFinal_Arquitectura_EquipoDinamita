using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace PuntoMarket.Frontend.Auth;

public sealed class ActivacionesPendientes(IMemoryCache cache, TimeSpan? vigencia = null)
{
    public const int LongitudDeReferencia = 64;
    public static readonly TimeSpan VigenciaPredeterminada = TimeSpan.FromMinutes(60);

    private const string Prefijo = "activacion:";

    public TimeSpan Vigencia { get; } = vigencia ?? VigenciaPredeterminada;

    public string Guardar(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var referencia = Convert.ToHexString(RandomNumberGenerator.GetBytes(LongitudDeReferencia / 2));
        cache.Set(Prefijo + referencia, token, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Vigencia
        });
        return referencia;
    }

    public string? Obtener(string? referencia) =>
        EsReferencia(referencia) ? cache.Get<string>(Prefijo + referencia) : null;

    public void Eliminar(string? referencia)
    {
        if (EsReferencia(referencia))
            cache.Remove(Prefijo + referencia);
    }

    public static bool EsReferencia(string? valor) =>
        valor is { Length: LongitudDeReferencia } && valor.All(Uri.IsHexDigit);
}
