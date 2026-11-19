using Proveedores.Domain.Entities;

namespace Proveedores.Tests;

public class ProveedorValidacionesTests
{
    [Theory]
    [InlineData("44112233")]
    [InlineData("71234567")]
    [InlineData("61234567")]
    [InlineData(" 71234567 ")]
    public void TelefonoValido_ConservaDigitosYPrefijo(string numero)
    {
        var r = ProveedorValidador.Validar(Datos.Valido(numero: numero));
        Assert.True(r.IsSuccess);
        Assert.Equal(numero.Trim(), r.Value.NumeroEmpresa);
    }

    [Theory]
    [InlineData("+59144112233")]
    [InlineData("+59171234567")]
    [InlineData("+59161234567")]
    [InlineData("59171234567")]
    [InlineData("12345678")]
    [InlineData("51234567")]
    [InlineData("81234567")]
    [InlineData("7123456")]
    [InlineData("712345678")]
    [InlineData("abc12345")]
    [InlineData("7123abcd")]
    [InlineData("+59112345678")]
    [InlineData("+5917123456")]
    [InlineData("71234567<script>")]
    [InlineData("7123 4567")]
    [InlineData("71234567\n")]
    [InlineData("\t71234567")]
    [InlineData("７１２３４５６７")]
    [InlineData("71234567\u200b")]
    public void TelefonoInvalido_NoSeReparaSilenciosamente(string numero)
    {
        var r = ProveedorValidador.Validar(Datos.Valido(numero: numero));
        Assert.Equal("Proveedor.NumeroInvalido", r.Error.Code);
        Assert.Contains("numeroEmpresa", r.Error.Fields!.Keys);
    }

    [Theory]
    [InlineData("PIL Andina S.A.")]
    [InlineData("EMBOL S.A. (Coca-Cola)")]
    [InlineData("Distribuidora 24/7")]
    [InlineData("Comercial Pérez & Hijos")]
    [InlineData("Industrias Venado S.A. (Kris)")]
    [InlineData("O'Reilly")]
    [InlineData("Script Comercial")]
    [InlineData("Marca® & Servicios™")]
    [InlineData("Comercial Ⅳ")]
    [InlineData("商店 24")]
    [InlineData("Cafe\u0301 del Sur")]
    public void NombreComercial_ConservaOrtografia(string nombre)
    {
        var r = ProveedorValidador.Validar(Datos.Valido(nombre));
        Assert.True(r.IsSuccess);
        Assert.Equal(nombre, r.Value.NombreEmpresa);
    }

    [Theory]
    [InlineData("@@@@@@")]
    [InlineData("--- & /")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("alert(1)")]
    [InlineData("Empresa\nSur")]
    [InlineData("Empresa\tSur")]
    [InlineData("Empresa\u202eSur")]
    [InlineData("Empresa\u200bSur")]
    [InlineData("Empresa\u034fSur")]
    [InlineData("Empresa\ufe0fSur")]
    [InlineData("\ud800")]
    public void NombreInvalido_DevuelveResultSinExcepcion(string nombre)
    {
        var r = ProveedorValidador.Validar(Datos.Valido(nombre));
        Assert.Equal("Proveedor.NombreInvalido", r.Error.Code);
    }

    [Theory]
    [InlineData("ventas@empresa.com")]
    [InlineData("ventas@empresa.com.bo")]
    [InlineData("ventas+cbba@empresa.com.bo")]
    [InlineData("contacto@subdominio.empresa.com")]
    [InlineData("  ventas@empresa.com  ")]
    public void CorreoValido_PermiteSubdominiosYMas(string correo)
    {
        var r = ProveedorValidador.Validar(Datos.Valido(correo: correo));
        Assert.True(r.IsSuccess);
        Assert.Equal(correo.Trim(), r.Value.CorreoReferencia);
    }

    [Theory]
    [InlineData("ventas@@empresa.com")]
    [InlineData("ventas empresa@gmail.com")]
    [InlineData("ventas@empresa")]
    [InlineData("ventas@empresa..com")]
    [InlineData("@empresa.com")]
    [InlineData("ventas@")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("ventas..cbba@empresa.com")]
    [InlineData(".ventas@empresa.com")]
    [InlineData("ventas.@empresa.com")]
    [InlineData("ventas@-empresa.com")]
    [InlineData("ventas@empresa-.com")]
    [InlineData("ventas@empresa.c")]
    [InlineData("ventas@empresa.com\r\n")]
    [InlineData("\t")]
    [InlineData("ventas@empresa.com\u200b")]
    public void CorreoInvalido_NoSeAlmacena(string correo)
    {
        Assert.Equal("Proveedor.CorreoInvalido", ProveedorValidador.Validar(Datos.Valido(correo: correo)).Error.Code);
    }

    [Fact]
    public void Limites_YErroresPorCampo()
    {
        var r = ProveedorValidador.Validar(new DatosProveedor(new string('a',151), new string('7',21), new string('a',151), false));
        Assert.Equal("Proveedor.DatosInvalidos", r.Error.Code);
        Assert.Equal(3, r.Error.Fields!.Count);
        Assert.True(ProveedorValidador.Validar(Datos.Valido(new string('a',150))).IsSuccess);
    }
}
