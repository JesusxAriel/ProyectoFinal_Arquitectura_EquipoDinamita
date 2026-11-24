using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PuntoMarket.Usuarios.Infrastructure.Security;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Seguridad;

public class GeneradorTokenActivacionTests
{
    private static readonly DateTime Ahora = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
    private readonly GeneradorTokenActivacion generador = new();

    [Fact]
    public void El_token_tiene_256_bits_en_base64_url_sin_relleno()
    {
        var token = generador.Generar(Ahora, TimeSpan.FromMinutes(60));

        Assert.Equal(43, token.Valor.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", token.Valor);
    }

    [Fact]
    public void Dos_tokens_nunca_coinciden()
    {
        var valores = Enumerable.Range(0, 200).Select(_ => generador.Generar(Ahora, TimeSpan.FromMinutes(60)).Valor);

        Assert.Equal(200, valores.Distinct().Count());
    }

    [Fact]
    public void Solo_se_entrega_el_hash_sha256_en_hexadecimal_y_no_el_valor()
    {
        var token = generador.Generar(Ahora, TimeSpan.FromMinutes(60));

        var esperado = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Valor)));
        Assert.Equal(esperado, token.Hash);
        Assert.Equal(64, token.Hash.Length);
        Assert.Matches("^[0-9A-F]{64}$", token.Hash);
        Assert.NotEqual(token.Valor, token.Hash);
        Assert.False(Regex.IsMatch(token.Hash, "[^0-9A-F]"));
    }

    [Fact]
    public void El_vencimiento_es_la_hora_actual_mas_la_vigencia()
    {
        var token = generador.Generar(Ahora, TimeSpan.FromMinutes(45));

        Assert.Equal(Ahora.AddMinutes(45), token.ExpiraUtc);
    }

    [Fact]
    public void El_hash_es_determinista_para_el_mismo_token()
    {
        Assert.Equal(generador.CalcularHash("abc"), generador.CalcularHash("abc"));
        Assert.NotEqual(generador.CalcularHash("abc"), generador.CalcularHash("abd"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void No_calcula_el_hash_de_un_token_vacio(string? token)
    {
        Assert.ThrowsAny<ArgumentException>(() => generador.CalcularHash(token!));
    }
}
