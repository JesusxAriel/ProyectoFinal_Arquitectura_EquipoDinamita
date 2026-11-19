using Proveedores.Application.Dtos;
using Proveedores.Application.UseCases;
using Proveedores.Domain.Errors;

namespace Proveedores.Tests;

public class ProveedorUseCasesTests
{
    [Fact]
    public async Task ActualizacionRechazada_NoModificaDatosPersistidos()
    {
        await Crear().EjecutarAsync(Datos.Request("Uno"), default);
        var dos = (await Crear().EjecutarAsync(Datos.Request("Dos"), default)).Value;
        var r = await Actualizar().EjecutarAsync(dos.Id, Datos.Request("Uno"), default);
        Assert.Equal(ProveedorErrors.NombreDuplicado, r.Error);
        var guardado = (await Obtener().EjecutarAsync(dos.Id, default)).Value;
        Assert.Equal("Dos", guardado.NombreEmpresa);
        Assert.Null(guardado.FechaActualizacion);
    }

    private readonly InMemoryProveedorRepository _repo = new();
    private readonly FakeCurrentUser _usuario = new("sub-admin-1");
    private readonly FixedTimeProvider _reloj = new(new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero));

    private CrearProveedor Crear() => new(_repo, _usuario, _reloj);
    private ActualizarProveedor Actualizar() => new(_repo, _usuario, _reloj);
    private DesactivarProveedor Desactivar() => new(_repo, _usuario, _reloj);
    private ReactivarProveedor Reactivar() => new(_repo, _usuario, _reloj);
    private ObtenerProveedor Obtener() => new(_repo);

    [Fact]
    public async Task Crear_Valido_PersisteYRegistraAuditoriaConSub()
    {
        var r = await Crear().EjecutarAsync(Datos.Request(), default);

        Assert.True(r.IsSuccess);
        Assert.False(string.IsNullOrEmpty(r.Value.Id));
        Assert.Equal("sub-admin-1", r.Value.IdUsuarioAdmin);
        Assert.Equal(_reloj.Ahora.UtcDateTime, r.Value.FechaCreacion);
        Assert.True(r.Value.EstaActivo);
        Assert.Single(_repo.Todos);
    }

    [Fact]
    public async Task Crear_Invalido_DevuelveFailureSinExcepcionNiPersistencia()
    {
        var r = await Crear().EjecutarAsync(Datos.Request(nombre: "  "), default);

        Assert.True(r.IsFailure);
        Assert.Equal(ProveedorErrors.NombreRequerido.Code, r.Error.Code);
        Assert.Contains("nombreEmpresa", r.Error.Fields!.Keys);
        Assert.Empty(_repo.Todos);
    }

    [Fact]
    public async Task Crear_NombreDuplicadoActivo_IgnorandoMayusculasYEspacios_Conflicto()
    {
        await Crear().EjecutarAsync(Datos.Request("Distribuidora Andina"), default);
        var r = await Crear().EjecutarAsync(Datos.Request("  DISTRIBUIDORA   andina "), default);

        Assert.True(r.IsFailure);
        Assert.Equal(ProveedorErrors.NombreDuplicado, r.Error);
        Assert.Single(_repo.Todos);
    }

    [Fact]
    public async Task Crear_SinUsuarioIdentificado_DevuelveNoAutenticado()
    {
        _usuario.UserId = null;
        var r = await Crear().EjecutarAsync(Datos.Request(), default);
        Assert.Equal(ProveedorErrors.UsuarioNoIdentificado, r.Error);
    }

    [Fact]
    public async Task Actualizar_Valido_RegistraAuditoriaDeActualizacion()
    {
        var creado = (await Crear().EjecutarAsync(Datos.Request(), default)).Value;
        _usuario.UserId = "sub-admin-2";
        _reloj.Ahora = _reloj.Ahora.AddHours(1);

        var r = await Actualizar().EjecutarAsync(creado.Id, Datos.Request("Nuevo Nombre", "70000999"), default);

        Assert.True(r.IsSuccess);
        Assert.Equal("Nuevo Nombre", r.Value.NombreEmpresa);
        Assert.Equal("sub-admin-2", r.Value.IdUsuarioActualizacion);
        Assert.Equal(_reloj.Ahora.UtcDateTime, r.Value.FechaActualizacion);
        Assert.Equal("sub-admin-1", r.Value.IdUsuarioAdmin); // el creador original no cambia
    }

    [Fact]
    public async Task Actualizar_ConNombreDeOtroProveedorActivo_Conflicto()
    {
        await Crear().EjecutarAsync(Datos.Request("Uno", "40000001"), default);
        var dos = (await Crear().EjecutarAsync(Datos.Request("Dos", "40000002"), default)).Value;

        var r = await Actualizar().EjecutarAsync(dos.Id, Datos.Request("uno", "40000002"), default);

        Assert.Equal(ProveedorErrors.NombreDuplicado, r.Error);
    }

    [Fact]
    public async Task Actualizar_ConservandoSuPropioNombre_Funciona()
    {
        var uno = (await Crear().EjecutarAsync(Datos.Request("Uno", "40000001"), default)).Value;
        var r = await Actualizar().EjecutarAsync(uno.Id, Datos.Request("Uno", "40000005"), default);
        Assert.True(r.IsSuccess);
    }

    [Fact]
    public async Task Actualizar_Inexistente_NoEncontrado()
    {
        var r = await Actualizar().EjecutarAsync("no-existe", Datos.Request(), default);
        Assert.Equal(ProveedorErrors.NoEncontrado, r.Error);
    }

    [Fact]
    public async Task Obtener_Inexistente_NoEncontrado()
    {
        var r = await Obtener().EjecutarAsync("no-existe", default);
        Assert.True(r.IsFailure);
        Assert.Equal(ProveedorErrors.NoEncontrado, r.Error);
    }

    [Fact]
    public async Task Desactivar_EsLogico_YRegistraAuditoria()
    {
        var creado = (await Crear().EjecutarAsync(Datos.Request(), default)).Value;
        var r = await Desactivar().EjecutarAsync(creado.Id, default);

        Assert.True(r.IsSuccess);
        Assert.Single(_repo.Todos); // el documento NO se elimina físicamente
        var guardado = (await Obtener().EjecutarAsync(creado.Id, default)).Value;
        Assert.False(guardado.EstaActivo);
        Assert.Equal("sub-admin-1", guardado.IdUsuarioActualizacion);
        Assert.NotNull(guardado.FechaActualizacion);
    }

    [Fact]
    public async Task Desactivar_YaDesactivado_Conflicto()
    {
        var creado = (await Crear().EjecutarAsync(Datos.Request(), default)).Value;
        await Desactivar().EjecutarAsync(creado.Id, default);

        var r = await Desactivar().EjecutarAsync(creado.Id, default);
        Assert.Equal(ProveedorErrors.YaDesactivado, r.Error);
    }

    [Fact]
    public async Task Desactivar_PermiteCrearOtroActivoConElMismoNombre()
    {
        var creado = (await Crear().EjecutarAsync(Datos.Request("Repetido"), default)).Value;
        await Desactivar().EjecutarAsync(creado.Id, default);

        var r = await Crear().EjecutarAsync(Datos.Request("Repetido"), default);
        Assert.True(r.IsSuccess);
    }

    [Fact]
    public async Task Reactivar_Desactivado_Funciona()
    {
        var creado = (await Crear().EjecutarAsync(Datos.Request(), default)).Value;
        await Desactivar().EjecutarAsync(creado.Id, default);

        var r = await Reactivar().EjecutarAsync(creado.Id, default);

        Assert.True(r.IsSuccess);
        Assert.True(r.Value.EstaActivo);
    }

    [Fact]
    public async Task Reactivar_ConNombreYaTomadoPorOtroActivo_RechazaPorConflicto()
    {
        var viejo = (await Crear().EjecutarAsync(Datos.Request("Repetido", "40000001"), default)).Value;
        await Desactivar().EjecutarAsync(viejo.Id, default);
        await Crear().EjecutarAsync(Datos.Request("Repetido", "40000002"), default);

        var r = await Reactivar().EjecutarAsync(viejo.Id, default);

        Assert.Equal(ProveedorErrors.ReactivacionConflictiva, r.Error);
    }

    [Fact]
    public async Task Reactivar_YaActivo_Conflicto()
    {
        var creado = (await Crear().EjecutarAsync(Datos.Request(), default)).Value;
        var r = await Reactivar().EjecutarAsync(creado.Id, default);
        Assert.Equal(ProveedorErrors.YaActivo, r.Error);
    }

    [Fact]
    public async Task Listar_BuscaOrdenaFiltraYPagina()
    {
        foreach (var (n, num) in new[] { ("Cafe Sur", "40000003"), ("Azucar Norte", "40000001"), ("Bebidas Este", "40000002"), ("Cafe Oeste", "40000004") })
            await Crear().EjecutarAsync(Datos.Request(n, num), default);
        var inactivo = (await Crear().EjecutarAsync(Datos.Request("Zeta Inactivo", "40000009"), default)).Value;
        await Desactivar().EjecutarAsync(inactivo.Id, default);

        var listar = new ListarProveedores(_repo);

        var asc = (await listar.EjecutarAsync(new ListarProveedoresQuery { Estado = EstadoFiltro.Activos }, default)).Value;
        Assert.Equal(new[] { "Azucar Norte", "Bebidas Este", "Cafe Oeste", "Cafe Sur" }, asc.Items.Select(i => i.NombreEmpresa));

        var desc = (await listar.EjecutarAsync(new ListarProveedoresQuery { Estado = EstadoFiltro.Activos, Descendente = true }, default)).Value;
        Assert.Equal("Cafe Sur", desc.Items[0].NombreEmpresa);

        var busqueda = (await listar.EjecutarAsync(new ListarProveedoresQuery { Busqueda = "CAFE" }, default)).Value;
        Assert.Equal(2, busqueda.Total);

        var inactivos = (await listar.EjecutarAsync(new ListarProveedoresQuery { Estado = EstadoFiltro.Inactivos }, default)).Value;
        Assert.Single(inactivos.Items);

        var pagina2 = (await listar.EjecutarAsync(new ListarProveedoresQuery { Estado = EstadoFiltro.Activos, Pagina = 2, TamanoPagina = 3 }, default)).Value;
        Assert.Single(pagina2.Items);
        Assert.Equal(4, pagina2.Total);
        Assert.Equal(2, pagina2.TotalPaginas);
    }

    [Fact]
    public async Task Listar_SaneaPaginacionInvalida()
    {
        await Crear().EjecutarAsync(Datos.Request(), default);
        var r = (await new ListarProveedores(_repo).EjecutarAsync(new ListarProveedoresQuery { Pagina = -4, TamanoPagina = 100000 }, default)).Value;
        Assert.Equal(1, r.Pagina);
        Assert.Equal(ListarProveedoresQuery.TamanoPaginaMaximo, r.TamanoPagina);
    }
}
