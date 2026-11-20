using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Entities;

namespace PuntoMarket.Clientes.Tests;

public class ClienteTests
{
    private static readonly DateTime Despues = Datos.Ahora.AddHours(1);

    [Fact]
    public void Actualizar_cambia_los_datos_y_registra_la_auditoria()
    {
        var c = Datos.NuevoCliente();
        var r = c.Actualizar(new DatosCliente("99999", "  Nuevo  Nombre ", null, null), "usuario-2", Despues);

        Assert.True(r.IsSuccess);
        Assert.Equal("99999", c.NitCi);
        Assert.Equal("Nuevo Nombre", c.RazonSocial);
        Assert.Equal("usuario-2", c.IdUsuarioActualizacion);
        Assert.Equal(Despues, c.FechaActualizacion);
    }

    [Fact]
    public void Actualizar_con_datos_invalidos_no_modifica_el_cliente()
    {
        var c = Datos.NuevoCliente();
        var r = c.Actualizar(new DatosCliente("", "", null, null), "usuario-2", Despues);

        Assert.True(r.IsFailure);
        Assert.Equal(ErrorType.Validation, r.Error.Type);
        Assert.Equal("4545451226", c.NitCi);
        Assert.Null(c.IdUsuarioActualizacion);
    }

    [Fact]
    public void El_cliente_generico_no_se_edita_ni_se_da_de_baja()
    {
        var g = Datos.Generico();
        Assert.Equal("Cliente.GenericoProtegido", g.Actualizar(Datos.Valido(), "u", Despues).Error.Code);
        Assert.Equal("Cliente.GenericoProtegido", g.DarDeBaja("u", Despues).Error.Code);
        Assert.True(g.EstaActivo);
    }

    [Fact]
    public void Baja_y_reactivacion_siguen_las_reglas_de_estado()
    {
        var c = Datos.NuevoCliente();

        Assert.True(c.DarDeBaja("u2", Despues).IsSuccess);
        Assert.False(c.EstaActivo);
        Assert.Equal("Cliente.YaInactivo", c.DarDeBaja("u2", Despues).Error.Code);
        Assert.Equal("Cliente.Inactivo", c.Actualizar(Datos.Valido(), "u2", Despues).Error.Code);

        Assert.True(c.Reactivar("u3", Despues).IsSuccess);
        Assert.True(c.EstaActivo);
        Assert.Equal("Cliente.YaActivo", c.Reactivar("u3", Despues).Error.Code);
    }

    [Fact]
    public void El_identificador_solo_se_asigna_una_vez()
    {
        var c = Datos.NuevoCliente();
        c.AsignarId(5);
        Assert.Equal(5, c.Id);
        Assert.Throws<InvalidOperationException>(() => c.AsignarId(6));
    }
}
