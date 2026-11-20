using PuntoMarket.Usuarios.Domain.Services;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Domain;

public class PoliticaDePasswordTests
{
    [Theory]
    [InlineData("Segura1!")]
    [InlineData("MiClave2026@")]
    [InlineData("Prueba#123A")]
    public void Acepta_contrasenas_que_cumplen_todos_los_requisitos(string contrasena)
    {
        Assert.True(PoliticaDePassword.Validar(contrasena).IsOk);
    }

    [Theory]
    [InlineData("Aa1!")]
    [InlineData("segura1!")]
    [InlineData("SEGURA1!")]
    [InlineData("Seguraaa!")]
    [InlineData("Segura12")]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechaza_contrasenas_que_incumplen_algun_requisito(string contrasena)
    {
        Assert.True(PoliticaDePassword.Validar(contrasena).IsFail);
    }

    [Fact]
    public void Rechaza_contrasena_nula()
    {
        Assert.True(PoliticaDePassword.Validar(null).IsFail);
    }

    [Fact]
    public void Explica_en_espanol_el_requisito_que_falta()
    {
        var resultado = PoliticaDePassword.Validar("segura1!");

        Assert.Equal("La contraseña debe tener al menos una letra mayúscula.", resultado.Error);
    }

    [Fact]
    public void Acepta_la_confirmacion_cuando_es_identica()
    {
        Assert.True(PoliticaDePassword.Coinciden("Segura1!", "Segura1!").IsOk);
    }

    [Theory]
    [InlineData("Segura1!", "segura1!")]
    [InlineData("Segura1!", "Segura1! ")]
    [InlineData("Segura1!", null)]
    public void Rechaza_la_confirmacion_cuando_difiere(string nueva, string? confirmacion)
    {
        Assert.True(PoliticaDePassword.Coinciden(nueva, confirmacion).IsFail);
    }
}
