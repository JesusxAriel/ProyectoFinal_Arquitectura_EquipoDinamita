using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Proveedores.Application.Dtos;
using Proveedores.Domain.Creators;
using Proveedores.Domain.Entities;
using Proveedores.Domain.Errors;
using Proveedores.Infrastructure.Mongo;

namespace Proveedores.Tests;

/// <summary>[Fact] que solo se ejecuta si PROVEEDORES_TEST_MONGO apunta a un MongoDB real (p. ej. mongodb://localhost:27017).</summary>
public sealed class MongoFactAttribute : FactAttribute
{
    public const string Variable = "PROVEEDORES_TEST_MONGO";

    public MongoFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable)))
            Skip = $"Prueba de integración: defina {Variable}=mongodb://localhost:27017 (ver README del microservicio).";
    }
}

/// <summary>
/// Pruebas de integración contra MongoDB real (índices y unicidad). Cada instancia usa una base de datos
/// aislada con nombre único que se elimina al terminar. Las pruebas unitarias NO dependen de esto.
/// </summary>
public class MongoPersistenceTests
{
    [MongoFact]
    public async Task Busqueda_PriorizaPrefijoLuegoNombreLuegoCorreo_AntesDePaginar_EnAmbosEstadosYOrdenes()
    {
        await PrepararAsync();
        try
        {
            foreach (var activo in new[] { true, false })
            {
                var sufijo = activo ? " Activo" : " Inactivo";
                foreach (var nombre in new[] { "Otro Alfa", "La Com Zeta", "Com Zeta", "Otro Zeta", "Com Alfa", "La Com Alfa" })
                {
                    var p = ProveedorCreatorSelector.Para(false).Crear(new DatosProveedor(nombre + sufijo,
                        "71234567", nombre.StartsWith("Otro") ? "com@empresa.test" : null, false), "test", DateTime.UtcNow).Value;
                    p = (await _repo!.AgregarAsync(p, default)).Value;
                    if (!activo) { Assert.True(p.Desactivar("test",DateTime.UtcNow).IsSuccess); Assert.True((await _repo.ActualizarAsync(p,default)).IsSuccess); }
                }
                foreach (var descendente in new[] { false, true })
                {
                    var esperados = descendente
                        ? new[] { "Com Zeta", "Com Alfa", "La Com Zeta", "La Com Alfa", "Otro Zeta", "Otro Alfa" }
                        : new[] { "Com Alfa", "Com Zeta", "La Com Alfa", "La Com Zeta", "Otro Alfa", "Otro Zeta" };
                    var ids = new List<string>();
                    for (var pagina = 1; pagina <= 3; pagina++)
                    {
                        var r = await _repo!.ListarAsync(new ListarProveedoresQuery { Busqueda = "COM", Descendente = descendente,
                            Estado = activo ? EstadoFiltro.Activos : EstadoFiltro.Inactivos, TamanoPagina = 2, Pagina = pagina }, default);
                        Assert.Equal(6,r.Total);
                        Assert.Equal(esperados.Skip((pagina-1)*2).Take(2).Select(n => n+sufijo),r.Items.Select(p => p.NombreEmpresa));
                        ids.AddRange(r.Items.Select(p => p.Id));
                    }
                    Assert.Equal(6,ids.Distinct().Count());
                }
            }
            // Teléfono también es coincidencia de menor prioridad que el nombre.
            await _repo!.AgregarAsync(Nuevo("Zeta 712"), default);
            await _repo.AgregarAsync(Nuevo("712 Empresa"), default);
            var telefonos = await _repo.ListarAsync(new ListarProveedoresQuery { Busqueda = "712", Estado = EstadoFiltro.Activos }, default);
            Assert.Equal("712 Empresa",telefonos.Items[0].NombreEmpresa);
            Assert.Equal("Zeta 712",telefonos.Items[1].NombreEmpresa);
        }
        finally { await LimpiarAsync(); }
    }

    [MongoFact]
    public async Task BusquedaLiteral_NoInterpretaRegexOperadoresNiSql_YBuscaCorreo()
    {
        await PrepararAsync();
        try
        {
            var p = ProveedorCreatorSelector.Para(false).Crear(new DatosProveedor("O'Reilly Comercial", "71234567",
                "ventas+cbba@sub.empresa.com", false), "test", DateTime.UtcNow).Value;
            await _repo!.AgregarAsync(p, default);
            foreach (var termino in new[] { ".*", "$ne", "' OR 1=1", "<script>", "[a-z]", "a|b", ".com$" })
            {
                var pagina = await _repo.ListarAsync(new ListarProveedoresQuery { Busqueda = termino }, default);
                Assert.Empty(pagina.Items);
                Assert.Equal(0, pagina.Total);
            }
            foreach (var termino in new[] { "O'Reilly", "ventas+cbba@sub.empresa.com", "71234567" })
            {
                var pagina = await _repo.ListarAsync(new ListarProveedoresQuery { Busqueda = termino }, default);
                Assert.Equal(p.Id, Assert.Single(pagina.Items).Id);
            }
        }
        finally { await LimpiarAsync(); }
    }

    [MongoFact]
    public async Task InactivosConNombreEquivalente_OrdenGlobalAntesDePaginar_DesempataPorId()
    {
        await PrepararAsync();
        try
        {
            var ids = new List<string>();
            foreach (var nombre in new[] { "Zeta", "Café", "CAFE", "Álfa" })
            {
                var p = (await _repo!.AgregarAsync(Nuevo(nombre), default)).Value;
                Assert.True(p.Desactivar("test", DateTime.UtcNow).IsSuccess);
                Assert.True((await _repo.ActualizarAsync(p, default)).IsSuccess);
                if (p.NombreNormalizado == "cafe") ids.Add(p.Id);
            }
            var primera = await _repo!.ListarAsync(new ListarProveedoresQuery { Estado = EstadoFiltro.Inactivos, TamanoPagina = 2 }, default);
            var segunda = await _repo.ListarAsync(new ListarProveedoresQuery { Estado = EstadoFiltro.Inactivos, TamanoPagina = 2, Pagina = 2 }, default);
            var todos = primera.Items.Concat(segunda.Items).ToArray();
            Assert.Equal(new[] { "alfa", "cafe", "cafe", "zeta" }, todos.Select(p => p.NombreNormalizado));
            Assert.Equal(ids.OrderBy(id => id), todos.Where(p => p.NombreNormalizado == "cafe").Select(p => p.Id));
            Assert.Equal(4, todos.Select(p => p.Id).Distinct().Count());
            var soloCafe = await _repo.ListarAsync(new ListarProveedoresQuery { Estado = EstadoFiltro.Inactivos,
                Busqueda = "cafe", TamanoPagina = 1, Pagina = 2 }, default);
            Assert.Equal(2, soloCafe.Total);
            Assert.Equal(ids.OrderBy(id => id).Last(), Assert.Single(soloCafe.Items).Id);
        }
        finally { await LimpiarAsync(); }
    }

    [MongoFact]
    public async Task EdicionObsoleta_NoRevierteUnaDesactivacion_YFuncionaSinVersionPrevia()
    {
        await PrepararAsync();
        try
        {
            var creado = (await _repo!.AgregarAsync(Nuevo("Estado concurrente"), default)).Value;
            // Simula un documento existente anterior al control de versiones.
            await _coleccion!.UpdateOneAsync(x => x.Id == MongoDB.Bson.ObjectId.Parse(creado.Id),
                Builders<ProveedorDocument>.Update.Unset(x => x.Version));
            var editor = await _repo.ObtenerPorIdAsync(creado.Id, default);
            var desactivador = await _repo.ObtenerPorIdAsync(creado.Id, default);
            desactivador!.Desactivar("admin-b", DateTime.UtcNow);
            Assert.True((await _repo.ActualizarAsync(desactivador, default)).IsSuccess);
            editor!.Actualizar(new DatosProveedor("Edicion obsoleta", "40000002", null, false), "admin-a", DateTime.UtcNow);
            var r = await _repo.ActualizarAsync(editor, default);
            Assert.Equal(ProveedorErrors.CambioConcurrente, r.Error);
            var final = await _repo.ObtenerPorIdAsync(creado.Id, default);
            Assert.False(final!.EstaActivo);
            Assert.Equal("Estado concurrente", final.NombreEmpresa);
            Assert.Equal("admin-b", final.IdUsuarioActualizacion);
            Assert.Equal(1, final.Version);
        }
        finally { await LimpiarAsync(); }
    }

    [MongoFact]
    public async Task EscriturasConcurrentes_EnRepositoriosDistintos_SoloUnaSeAcepta()
    {
        await PrepararAsync();
        try
        {
            var creado = (await _repo!.AgregarAsync(Nuevo("Original"), default)).Value;
            var repos = Enumerable.Range(0, 8).Select(_ => new MongoProveedorRepository(
                _coleccion!, NullLogger<MongoProveedorRepository>.Instance)).ToArray();
            var copias = await Task.WhenAll(repos.Select(r => r.ObtenerPorIdAsync(creado.Id, default)));
            for (var i = 0; i < copias.Length; i++)
                copias[i]!.Actualizar(new DatosProveedor($"Cambio {i}", "40000001", null, false), $"admin-{i}", DateTime.UtcNow);
            var resultados = await Task.WhenAll(repos.Select((r, i) => r.ActualizarAsync(copias[i]!, default)));
            Assert.Equal(1, resultados.Count(r => r.IsSuccess));
            Assert.Equal(7, resultados.Count(r => r.Error == ProveedorErrors.CambioConcurrente));
            Assert.Equal(1, (await _repo.ObtenerPorIdAsync(creado.Id, default))!.Version);
        }
        finally { await LimpiarAsync(); }
    }

    [MongoFact]
    public async Task DesactivacionObsoleta_NoSobrescribeUnaEdicion()
    {
        await PrepararAsync();
        try
        {
            var creado = (await _repo!.AgregarAsync(Nuevo("Inicial"), default)).Value;
            var editor = await _repo.ObtenerPorIdAsync(creado.Id, default);
            var baja = await _repo.ObtenerPorIdAsync(creado.Id, default);
            editor!.Actualizar(new DatosProveedor("Editado", "70000099", null, false), "editor", DateTime.UtcNow);
            Assert.True((await _repo.ActualizarAsync(editor, default)).IsSuccess);
            baja!.Desactivar("baja", DateTime.UtcNow);
            Assert.Equal(ProveedorErrors.CambioConcurrente, (await _repo.ActualizarAsync(baja, default)).Error);
            var final = await _repo.ObtenerPorIdAsync(creado.Id, default);
            Assert.True(final!.EstaActivo);
            Assert.Equal("Editado", final.NombreEmpresa);
            Assert.Equal("70000099", final.NumeroEmpresa);
        }
        finally { await LimpiarAsync(); }
    }

    [MongoFact]
    public async Task PaginacionExtrema_DevuelveVacioSinDesbordar()
    {
        await PrepararAsync();
        try
        {
            await _repo!.AgregarAsync(Nuevo("Uno"), default);
            var pagina = await _repo.ListarAsync(new ListarProveedoresQuery { Pagina = int.MaxValue, TamanoPagina = 100 }, default);
            Assert.Empty(pagina.Items);
            Assert.Equal(1, pagina.Total);
        }
        finally { await LimpiarAsync(); }
    }

    private readonly string _nombreBd = $"proveedores_test_{Guid.NewGuid():N}";
    private IMongoClient? _cliente;
    private IMongoCollection<ProveedorDocument>? _coleccion;
    private MongoProveedorRepository? _repo;

    private static string? Uri => Environment.GetEnvironmentVariable(MongoFactAttribute.Variable);

    private async Task PrepararAsync(bool seed = false)
    {
        _cliente = new MongoClient(Uri);
        _coleccion = _cliente.GetDatabase(_nombreBd).GetCollection<ProveedorDocument>("proveedores");
        _repo = new MongoProveedorRepository(_coleccion, NullLogger<MongoProveedorRepository>.Instance);
        await Inicializador(seed).StartAsync(default);
    }

    private MongoStartupInitializer Inicializador(bool seed) => new(
        _coleccion!,
        Options.Create(new MongoSettings { InitializeOnStartup = true, SeedEnabled = seed }),
        NullLogger<MongoStartupInitializer>.Instance);

    private async Task LimpiarAsync()
    {
        if (_cliente is not null) await _cliente.DropDatabaseAsync(_nombreBd);
    }

    private static Proveedor Nuevo(string nombre, string numero = "40000001") =>
        ProveedorCreatorSelector.Para(false).Crear(new DatosProveedor(nombre, numero, null, false), "sub-test", DateTime.UtcNow).Value;

    [MongoFact]
    public async Task Inicializacion_CreaIndiceUnicoParcial()
    {
        await PrepararAsync();
        try
        {
            var indices = await (await _coleccion!.Indexes.ListAsync()).ToListAsync();
            var unico = indices.Single(i => i["name"] == ProveedorIndexes.NombreActivoUnico);
            Assert.True(unico["unique"].AsBoolean);
            Assert.True(unico.Contains("partialFilterExpression"));
        }
        finally { await LimpiarAsync(); }
    }

    [MongoFact]
    public async Task NoPermiteDosProveedoresActivosConElMismoNombre()
    {
        await PrepararAsync();
        try
        {
            Assert.True((await _repo!.AgregarAsync(Nuevo("Mismo Nombre"), default)).IsSuccess);

            var segundo = await _repo.AgregarAsync(Nuevo("  MISMO   nombre "), default);

            Assert.True(segundo.IsFailure);
            Assert.Equal(ProveedorErrors.NombreDuplicado, segundo.Error);
        }
        finally { await LimpiarAsync(); }
    }

    [MongoFact]
    public async Task PermiteDuplicarNombreSiElOtroEstaInactivo_YBloqueaLaReactivacion()
    {
        await PrepararAsync();
        try
        {
            var primero = (await _repo!.AgregarAsync(Nuevo("Repetido"), default)).Value;
            primero.Desactivar("sub-test", DateTime.UtcNow);
            Assert.True((await _repo.ActualizarAsync(primero, default)).IsSuccess);

            Assert.True((await _repo.AgregarAsync(Nuevo("Repetido"), default)).IsSuccess);

            primero.Reactivar("sub-test", DateTime.UtcNow);
            var reactivacion = await _repo.ActualizarAsync(primero, default);
            Assert.Equal(ProveedorErrors.NombreDuplicado, reactivacion.Error);
        }
        finally { await LimpiarAsync(); }
    }

    [MongoFact]
    public async Task InsercionesConcurrentes_SoloUnaTieneExito()
    {
        await PrepararAsync();
        try
        {
            var tareas = Enumerable.Range(0, 8).Select(_ => _repo!.AgregarAsync(Nuevo("Concurrente"), default));
            var resultados = await Task.WhenAll(tareas);

            Assert.Equal(1, resultados.Count(r => r.IsSuccess));
            Assert.Equal(7, resultados.Count(r => r.IsFailure && r.Error == ProveedorErrors.NombreDuplicado));
            Assert.Equal(1, await _coleccion!.CountDocumentsAsync(_ => true));
        }
        finally { await LimpiarAsync(); }
    }

    [MongoFact]
    public async Task Listado_FiltraOrdenaPaginaYBuscaSinTildes()
    {
        await PrepararAsync();
        try
        {
            foreach (var (n, num) in new[] { ("Café Sur", "40000003"), ("Azúcar Norte", "40000001"), ("Bebidas Este", "40000002") })
                await _repo!.AgregarAsync(Nuevo(n, num), default);

            var porNombre = await _repo!.ListarAsync(new ListarProveedoresQuery { Pagina = 1, TamanoPagina = 10 }, default);
            Assert.Equal(new[] { "Azúcar Norte", "Bebidas Este", "Café Sur" }, porNombre.Items.Select(p => p.NombreEmpresa));

            var busqueda = await _repo.ListarAsync(new ListarProveedoresQuery { Busqueda = "cafe", Pagina = 1, TamanoPagina = 10 }, default);
            Assert.Single(busqueda.Items);

            var pagina = await _repo.ListarAsync(new ListarProveedoresQuery { Pagina = 2, TamanoPagina = 2 }, default);
            Assert.Single(pagina.Items);
            Assert.Equal(3, pagina.Total);
        }
        finally { await LimpiarAsync(); }
    }

    [MongoFact]
    public async Task Seed_EsIdempotente_YNoSobrescribeCambios()
    {
        await PrepararAsync(seed: true);
        try
        {
            Assert.Equal(7, await _coleccion!.CountDocumentsAsync(_ => true));
            var esperados = new[]
            {
                ("PIL Andina S.A.", "44112233", "ventas.cbba@pilandina.com.bo", false),
                ("EMBOL S.A. (Coca-Cola)", "44556677", "pedidos.cbba@embol.com.bo", false),
                ("Sofía SHE S.A.", "44889900", "contacto.cbba@sofia.com.bo", true),
                ("CBN (Cervecería Boliviana Nacional)", "44224466", "distribucion@cbn.com.bo", false),
                ("Unilever Bolivia", "44335577", "atencion@unilever.com", false),
                ("Industrias Venado S.A. (Kris)", "44118899", "ventas@venado.com.bo", false),
                ("Arcor Bolivia", "44778811", "pedidos@arcor.com.bo", false)
            };
            for (var i = 0; i < esperados.Length; i++)
            {
                var marca = $"seed-proveedor-{i + 1}";
                var doc = await _coleccion.Find(x => x.OrigenSeed == marca).SingleAsync();
                var (nombre, numero, correo, auto) = esperados[i];
                Assert.Equal(nombre, doc.NombreEmpresa);
                Assert.Equal(numero, doc.NumeroEmpresa);
                Assert.Equal(correo, doc.CorreoReferencia);
                Assert.Equal(auto, doc.EsAutogestionado);
                Assert.Equal(NombreNormalizador.Normalizar(nombre), doc.NombreNormalizado);
                Assert.True(doc.EstaActivo);
                Assert.Equal(ProveedorSeeder.UsuarioSistema, doc.IdUsuarioAdmin);
                Assert.Equal(0, doc.Version);
                Assert.NotEqual(MongoDB.Bson.ObjectId.Empty, doc.Id);
                Assert.Null(doc.FechaActualizacion);
                Assert.Null(doc.IdUsuarioActualizacion);
            }

            await _repo!.AgregarAsync(Nuevo("Registro manual"), default);
            var primero = await _coleccion.Find(x => x.OrigenSeed == "seed-proveedor-1").SingleAsync();
            var editado = await _repo.ObtenerPorIdAsync(primero.Id.ToString(), default);
            editado!.Actualizar(new DatosProveedor("Proveedor editado manualmente", "70009999", null, false), "admin-test", DateTime.UtcNow);
            editado.Desactivar("admin-test", DateTime.UtcNow);
            Assert.True((await _repo.ActualizarAsync(editado, default)).IsSuccess);
            var antes = (await _coleccion.Find(_ => true).SortBy(x => x.Id).ToListAsync())
                .Select(d => d.ToBsonDocument().ToJson()).ToArray();

            await Inicializador(seed: true).StartAsync(default); // "reinicio"

            Assert.Equal(8, await _coleccion.CountDocumentsAsync(_ => true));
            var uno = await _coleccion.Find(x => x.OrigenSeed == "seed-proveedor-1").FirstAsync();
            Assert.Equal("70009999", uno.NumeroEmpresa);
            var despues = (await _coleccion.Find(_ => true).SortBy(x => x.Id).ToListAsync())
                .Select(d => d.ToBsonDocument().ToJson()).ToArray();
            Assert.Equal(antes, despues); // Incluye ObjectId, estado, auditoría, versión y registro manual.
        }
        finally { await LimpiarAsync(); }
    }
}
