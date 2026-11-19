using Proveedores.Domain.Creators;
using Proveedores.Domain.Entities;

namespace Proveedores.Tests;

/// <summary>Pruebas del patrón Factory Method combinado con Result.</summary>
public class ProveedorCreatorTests
{
    private static readonly DateTime Ahora = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Crear_DatosValidos_DevuelveProveedorConAuditoria()
    {
        var r = new ProveedorEstandarCreator().Crear(Datos.Valido(), "user-1", Ahora);

        Assert.True(r.IsSuccess);
        Assert.Equal("Distribuidora Andina", r.Value.NombreEmpresa);
        Assert.True(r.Value.EstaActivo);
        Assert.Equal("user-1", r.Value.IdUsuarioAdmin);
        Assert.Equal(Ahora, r.Value.FechaCreacion);
        Assert.Null(r.Value.FechaActualizacion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("     ")]
    public void Crear_NombreVacioOSoloEspacios_Falla(string? nombre)
    {
        var r = new ProveedorEstandarCreator().Crear(Datos.Valido(nombre!), "u", Ahora);
        Assert.True(r.IsFailure);
        Assert.Equal("Proveedor.NombreRequerido", r.Error.Code);
    }

    [Fact]
    public void Crear_NombreMasLargoQue150_Falla()
    {
        var r = new ProveedorEstandarCreator().Crear(Datos.Valido(new string('a', 151)), "u", Ahora);
        Assert.Equal("Proveedor.NombreMuyLargo", r.Error.Code);
    }

    [Fact]
    public void Crear_NumeroVacio_Falla()
    {
        var r = new ProveedorEstandarCreator().Crear(Datos.Valido(numero: " "), "u", Ahora);
        Assert.Equal("Proveedor.NumeroRequerido", r.Error.Code);
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("a@b")]
    [InlineData("dos@@arrobas.com")]
    [InlineData("con espacio@dominio.com")]
    public void Crear_CorreoInvalido_Falla(string correo)
    {
        var r = new ProveedorEstandarCreator().Crear(Datos.Valido(correo: correo), "u", Ahora);
        Assert.True(r.IsFailure);
        Assert.Equal("Proveedor.CorreoInvalido", r.Error.Code);
    }

    [Fact]
    public void Crear_CorreoOpcional_AceptaNuloOVacio()
    {
        Assert.True(new ProveedorEstandarCreator().Crear(Datos.Valido(correo: null), "u", Ahora).IsSuccess);
        var vacio = new ProveedorEstandarCreator().Crear(Datos.Valido(correo: "  "), "u", Ahora);
        Assert.True(vacio.IsSuccess);
        Assert.Null(vacio.Value.CorreoReferencia);
    }

    [Fact]
    public void Crear_NormalizaEspaciosYRecortaDatos()
    {
        var r = new ProveedorEstandarCreator().Crear(
            Datos.Valido("  Distribuidora    Andina  ", " 70012345 ", "  contacto@andina.example.invalid "), "u", Ahora);

        Assert.Equal("Distribuidora Andina", r.Value.NombreEmpresa);
        Assert.Equal("70012345", r.Value.NumeroEmpresa);
        Assert.Equal("contacto@andina.example.invalid", r.Value.CorreoReferencia);
    }

    [Fact]
    public void NombreNormalizado_IgnoraMayusculasTildesYEspacios()
    {
        var a = new ProveedorEstandarCreator().Crear(Datos.Valido("  DISTRIBUIDORA   Ándina "), "u", Ahora).Value;
        var b = new ProveedorEstandarCreator().Crear(Datos.Valido("distribuidora andina"), "u", Ahora).Value;
        Assert.Equal(b.NombreNormalizado, a.NombreNormalizado);
    }

    [Fact]
    public void VariasViolaciones_SeInformanJuntas()
    {
        var r = new ProveedorEstandarCreator().Crear(new DatosProveedor("", "", "malo", false), "u", Ahora);
        Assert.True(r.IsFailure);
        Assert.Equal("Proveedor.DatosInvalidos", r.Error.Code);
    }

    [Fact]
    public void CreadoresConcretos_RedefinenElMetodoFabrica()
    {
        var estandar = new ProveedorEstandarCreator().Crear(Datos.Valido(autogestionado: true), "u", Ahora).Value;
        var auto = new ProveedorAutogestionadoCreator().Crear(Datos.Valido(autogestionado: false), "u", Ahora).Value;

        Assert.False(estandar.EsAutogestionado);
        Assert.True(auto.EsAutogestionado);
    }

    [Fact]
    public void Selector_EligeElCreadorSegunElTipo()
    {
        Assert.IsType<ProveedorAutogestionadoCreator>(ProveedorCreatorSelector.Para(true));
        Assert.IsType<ProveedorEstandarCreator>(ProveedorCreatorSelector.Para(false));
    }
}
