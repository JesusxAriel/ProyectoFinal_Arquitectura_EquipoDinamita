using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Entities;
using PuntoMarket.Clientes.Domain.Validaciones;

namespace PuntoMarket.Clientes.Tests;

public class ClienteValidadorTests
{
    [Fact]
    public void Datos_validos_se_limpian_y_aceptan()
    {
        var r = ClienteValidador.Validar(new DatosCliente("  4545451226 ", "  Juan   Pérez  Vargas ", " a@b.com ", " 77123456 "));
        Assert.True(r.IsSuccess);
        Assert.Equal("4545451226", r.Value.NitCi);
        Assert.Equal("Juan Pérez Vargas", r.Value.RazonSocial);
        Assert.Equal("a@b.com", r.Value.Correo);
        Assert.Equal("77123456", r.Value.Telefono);
    }

    [Fact]
    public void Correo_y_telefono_en_blanco_se_convierten_en_null()
    {
        var r = ClienteValidador.Validar(new DatosCliente("12345", "Ana", "   ", ""));
        Assert.True(r.IsSuccess);
        Assert.Null(r.Value.Correo);
        Assert.Null(r.Value.Telefono);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234")]               // muy corto
    [InlineData("1234567890123456")]   // 16 dígitos
    [InlineData("12A45")]              // letras
    [InlineData("12 345")]             // espacio interior
    public void NitCi_invalido_se_rechaza(string nitCi)
    {
        var r = ClienteValidador.Validar(new DatosCliente(nitCi, "Ana", null, null));
        Assert.True(r.IsFailure);
        Assert.Equal(ErrorType.Validation, r.Error.Type);
        Assert.Contains("nitCi", r.Error.Fields!.Keys);
    }

    [Fact]
    public void El_nit_cero_esta_reservado_salvo_para_el_creador_generico()
    {
        var datos = new DatosCliente("0", "Sin Nombre", null, null);
        Assert.True(ClienteValidador.Validar(datos).IsFailure);
        Assert.True(ClienteValidador.Validar(datos, permitirGenerico: true).IsSuccess);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<script>")]
    public void RazonSocial_invalida_se_rechaza(string razon)
    {
        var r = ClienteValidador.Validar(new DatosCliente("12345", razon, null, null));
        Assert.True(r.IsFailure);
        Assert.Contains("razonSocial", r.Error.Fields!.Keys);
    }

    [Fact]
    public void RazonSocial_de_mas_de_150_caracteres_se_rechaza()
    {
        var r = ClienteValidador.Validar(new DatosCliente("12345", new string('a', 151), null, null));
        Assert.True(r.IsFailure);
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("a@b")]
    [InlineData("a@@b.com")]
    public void Correo_invalido_se_rechaza(string correo)
    {
        var r = ClienteValidador.Validar(new DatosCliente("12345", "Ana", correo, null));
        Assert.True(r.IsFailure);
        Assert.Contains("correo", r.Error.Fields!.Keys);
    }

    [Theory]
    [InlineData("12345678")]    // empieza con 1
    [InlineData("7712345")]     // 7 dígitos
    [InlineData("+59177123456")]
    public void Telefono_invalido_se_rechaza(string telefono)
    {
        var r = ClienteValidador.Validar(new DatosCliente("12345", "Ana", null, telefono));
        Assert.True(r.IsFailure);
        Assert.Contains("telefono", r.Error.Fields!.Keys);
    }

    [Fact]
    public void Acumula_todos_los_errores_en_una_sola_respuesta()
    {
        var r = ClienteValidador.Validar(new DatosCliente("", "", "mal", "123"));
        Assert.True(r.IsFailure);
        Assert.Equal(4, r.Error.Fields!.Count);
    }
}
