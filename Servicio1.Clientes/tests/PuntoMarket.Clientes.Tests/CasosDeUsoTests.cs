using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Application.UseCases;
using PuntoMarket.Clientes.Domain.Common;

namespace PuntoMarket.Clientes.Tests;

public class CasosDeUsoTests
{
    private readonly RepositorioEnMemoria _repo = new();
    private readonly TimeProvider _reloj = TimeProvider.System;

    private static GuardarClienteRequest Request(string nit = "4545451226", string razon = "Juan Pérez") =>
        new() { NitCi = nit, RazonSocial = razon, Correo = "jperez@gmail.com", Telefono = "77123456" };

    private CrearCliente Crear(string? usuario = "usuario-1") => new(_repo, new UsuarioFalso(usuario), _reloj);

    // ---------- Crear ----------

    [Fact]
    public async Task Crear_guarda_el_cliente_con_el_usuario_del_token()
    {
        var r = await Crear().EjecutarAsync(Request(), default);

        Assert.True(r.IsSuccess);
        Assert.True(r.Value.Id > 0);
        Assert.Equal("usuario-1", r.Value.IdUsuarioCreacion);
        Assert.Single(_repo.Todos);
    }

    [Fact]
    public async Task Crear_sin_usuario_identificado_devuelve_401()
    {
        var r = await Crear(usuario: null).EjecutarAsync(Request(), default);
        Assert.Equal(ErrorType.Unauthorized, r.Error.Type);
        Assert.Empty(_repo.Todos);
    }

    [Fact]
    public async Task Crear_con_datos_invalidos_devuelve_validacion_y_no_guarda()
    {
        var r = await Crear().EjecutarAsync(new GuardarClienteRequest(), default);
        Assert.Equal(ErrorType.Validation, r.Error.Type);
        Assert.Empty(_repo.Todos);
    }

    [Fact]
    public async Task Crear_con_nit_duplicado_entre_activos_devuelve_conflicto()
    {
        await Crear().EjecutarAsync(Request(), default);
        var r = await Crear().EjecutarAsync(Request(razon: "Otro nombre"), default);

        Assert.Equal(ErrorType.Conflict, r.Error.Type);
        Assert.Equal("Cliente.NitCiDuplicado", r.Error.Code);
        Assert.Single(_repo.Todos);
    }

    [Fact]
    public async Task Crear_con_el_nit_reservado_es_un_error_de_validacion()
    {
        var r = await Crear().EjecutarAsync(Request(nit: "0"), default);
        Assert.Equal(ErrorType.Validation, r.Error.Type);
    }

    // ---------- Obtener / Listar ----------

    [Fact]
    public async Task Obtener_inexistente_devuelve_404()
    {
        var r = await new ObtenerCliente(_repo).EjecutarAsync(99, default);
        Assert.Equal(ErrorType.NotFound, r.Error.Type);
    }

    [Fact]
    public async Task Listar_pagina_y_filtra_por_estado()
    {
        _repo.Sembrar(Datos.NuevoCliente("11111", "Ana"));
        _repo.Sembrar(Datos.NuevoCliente("22222", "Beto"));
        var inactivo = _repo.Sembrar(Datos.NuevoCliente("33333", "Carla"));
        inactivo.DarDeBaja("u", Datos.Ahora);

        var activos = await new ListarClientes(_repo).EjecutarAsync(
            new ListarClientesQuery { Estado = EstadoFiltro.Activos, TamanoPagina = 1 }, default);

        Assert.True(activos.IsSuccess);
        Assert.Equal(2, activos.Value.Total);
        Assert.Equal(2, activos.Value.TotalPaginas);
        Assert.Single(activos.Value.Items);
    }

    [Fact]
    public async Task Listar_con_busqueda_demasiado_larga_devuelve_validacion()
    {
        var r = await new ListarClientes(_repo).EjecutarAsync(new ListarClientesQuery { Busqueda = new string('x', 151) }, default);
        Assert.Equal(ErrorType.Validation, r.Error.Type);
    }

    // ---------- Actualizar ----------

    [Fact]
    public async Task Actualizar_cambia_los_datos_y_registra_quien_modifico()
    {
        var existente = _repo.Sembrar(Datos.NuevoCliente());
        var caso = new ActualizarCliente(_repo, new UsuarioFalso("usuario-9"), _reloj);

        var r = await caso.EjecutarAsync(existente.Id, Request(razon: "Nombre Nuevo"), default);

        Assert.True(r.IsSuccess);
        Assert.Equal("Nombre Nuevo", r.Value.RazonSocial);
        Assert.Equal("usuario-9", r.Value.IdUsuarioActualizacion);
        Assert.NotNull(r.Value.FechaActualizacion);
    }

    [Fact]
    public async Task Actualizar_a_un_nit_ya_usado_por_otro_activo_devuelve_conflicto()
    {
        _repo.Sembrar(Datos.NuevoCliente("11111", "Ana"));
        var otro = _repo.Sembrar(Datos.NuevoCliente("22222", "Beto"));
        var caso = new ActualizarCliente(_repo, new UsuarioFalso(), _reloj);

        var r = await caso.EjecutarAsync(otro.Id, Request(nit: "11111"), default);

        Assert.Equal("Cliente.NitCiDuplicado", r.Error.Code);
    }

    [Fact]
    public async Task Actualizar_inexistente_devuelve_404()
    {
        var caso = new ActualizarCliente(_repo, new UsuarioFalso(), _reloj);
        var r = await caso.EjecutarAsync(99, Request(), default);
        Assert.Equal(ErrorType.NotFound, r.Error.Type);
    }

    [Fact]
    public async Task Actualizar_el_cliente_generico_devuelve_conflicto()
    {
        var generico = _repo.Sembrar(Datos.Generico());
        var caso = new ActualizarCliente(_repo, new UsuarioFalso(), _reloj);

        var r = await caso.EjecutarAsync(generico.Id, Request(), default);

        Assert.Equal("Cliente.GenericoProtegido", r.Error.Code);
    }

    // ---------- Baja y reactivación ----------

    [Fact]
    public async Task Desactivar_es_baja_logica_y_el_cliente_se_conserva()
    {
        var c = _repo.Sembrar(Datos.NuevoCliente());
        var r = await new DesactivarCliente(_repo, new UsuarioFalso(), _reloj).EjecutarAsync(c.Id, default);

        Assert.True(r.IsSuccess);
        Assert.False(c.EstaActivo);
        Assert.Single(_repo.Todos);
    }

    [Fact]
    public async Task Desactivar_el_generico_devuelve_conflicto()
    {
        var g = _repo.Sembrar(Datos.Generico());
        var r = await new DesactivarCliente(_repo, new UsuarioFalso(), _reloj).EjecutarAsync(g.Id, default);
        Assert.Equal("Cliente.GenericoProtegido", r.Error.Code);
    }

    [Fact]
    public async Task Reactivar_funciona_si_el_nit_sigue_libre()
    {
        var c = _repo.Sembrar(Datos.NuevoCliente());
        c.DarDeBaja("u", Datos.Ahora);

        var r = await new ReactivarCliente(_repo, new UsuarioFalso(), _reloj).EjecutarAsync(c.Id, default);

        Assert.True(r.IsSuccess);
        Assert.True(c.EstaActivo);
    }

    [Fact]
    public async Task Reactivar_con_el_nit_tomado_por_otro_activo_devuelve_conflicto()
    {
        var viejo = _repo.Sembrar(Datos.NuevoCliente("11111", "Ana"));
        viejo.DarDeBaja("u", Datos.Ahora);
        _repo.Sembrar(Datos.NuevoCliente("11111", "Ana Nueva"));

        var r = await new ReactivarCliente(_repo, new UsuarioFalso(), _reloj).EjecutarAsync(viejo.Id, default);

        Assert.Equal("Cliente.NitCiDuplicado", r.Error.Code);
    }
}
