using System.Security.Cryptography;
using System.Text;
using PuntoMarket.Usuarios.Application.Ports.Gestion;

namespace PuntoMarket.Usuarios.Infrastructure.Security;

public sealed class GeneradorTokenActivacion : IGeneradorTokenActivacion
{
    private const int BytesDelToken = 32;

    public TokenActivacion Generar(DateTime ahoraUtc, TimeSpan vigencia)
    {
        var valor = Convert.ToBase64String(RandomNumberGenerator.GetBytes(BytesDelToken))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return new TokenActivacion(valor, CalcularHash(valor), ahoraUtc + vigencia);
    }

    public string CalcularHash(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
