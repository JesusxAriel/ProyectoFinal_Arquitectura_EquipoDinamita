using PuntoMarket.Clientes.Domain.Creators;
using PuntoMarket.Clientes.Domain.Entities;

namespace PuntoMarket.Clientes.Tests;

public class ClienteCreatorTests
{
    [Fact]
    public void El_selector_devuelve_el_creador_concreto_segun_el_tipo()
    {
        Assert.IsType<ClienteEstandarCreator>(ClienteCreatorSelector.Para(TipoCliente.Estandar));
        Assert.IsType<ClienteGenericoCreator>(ClienteCreatorSelector.Para(TipoCliente.Generico));
    }

    [Fact]
    public void El_creador_estandar_crea_un_cliente_activo_con_auditoria()
    {
        var r = new ClienteEstandarCreator().Crear(Datos.Valido(), "usuario-1", Datos.Ahora);
        Assert.True(r.IsSuccess);
        Assert.True(r.Value.EstaActivo);
        Assert.False(r.Value.EsGenerico);
        Assert.Equal("usuario-1", r.Value.IdUsuarioCreacion);
        Assert.Equal(Datos.Ahora, r.Value.FechaCreacion);
        Assert.Null(r.Value.FechaActualizacion);
    }

    [Fact]
    public void El_creador_estandar_rechaza_el_nit_reservado()
    {
        var r = new ClienteEstandarCreator().Crear(new DatosCliente("0", "Falso genérico", null, null), "u", Datos.Ahora);
        Assert.True(r.IsFailure);
    }

    [Fact]
    public void El_creador_generico_crea_el_cliente_sin_nombre()
    {
        var generico = Datos.Generico();
        Assert.True(generico.EsGenerico);
        Assert.Equal(Cliente.NitCiGenerico, generico.NitCi);
    }
}
