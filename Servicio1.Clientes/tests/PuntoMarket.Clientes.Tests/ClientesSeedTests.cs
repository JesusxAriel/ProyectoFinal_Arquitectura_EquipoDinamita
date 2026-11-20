using Microsoft.Extensions.Logging.Abstractions;
using PuntoMarket.Clientes.Adapters.Out.Persistence.Initialization;

namespace PuntoMarket.Clientes.Tests;

public class ClientesSeedTests
{
    [Fact]
    public void OpcionPorDefecto_ConservaCompatibilidad()
    {
        var opciones = new ClientesDbOptions();
        Assert.True(opciones.SeedEnabled);
        Assert.True(opciones.SeedGenericoEnabled);
    }

    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 2)]
    public async Task TablaVacia_SiembraSoloLosTiposConfigurados(bool incluirGenerico, int cantidad)
    {
        var repo = new RepositorioEnMemoria();
        await Sembrar(repo, incluirGenerico);
        Assert.Equal(cantidad, repo.Todos.Count);
        Assert.Equal(incluirGenerico, repo.Todos.Any(c => c.EsGenerico));
        Assert.Equal(new[] { "4545451226", "1020304050" }, repo.Todos.Where(c => !c.EsGenerico).Select(c => c.NitCi));
        Assert.All(repo.Todos, c => { Assert.True(c.EstaActivo); Assert.Equal("sistema", c.IdUsuarioCreacion); });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reinicio_NoRecreaGenericoNiCambiaClientesExistentes(bool incluirGenerico)
    {
        var repo = new RepositorioEnMemoria();
        await Sembrar(repo, incluirGenerico);
        var antes = repo.Todos.Select(c => (c.Id,c.NitCi,c.RazonSocial,c.FechaCreacion,c.FechaActualizacion)).ToArray();
        // Simula un nuevo arranque con el conteo de la tabla ya poblada, incluso cambiando la opción.
        await Sembrar(repo, !incluirGenerico);
        Assert.Equal(antes, repo.Todos.Select(c => (c.Id,c.NitCi,c.RazonSocial,c.FechaCreacion,c.FechaActualizacion)).ToArray());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DatosPrevios_SeConservanSinAgregarSemillas(bool genericoExistente)
    {
        var repo = new RepositorioEnMemoria();
        var normal = repo.Sembrar(Datos.NuevoCliente("12345678", "CLIENTE EXISTENTE"));
        if (genericoExistente) repo.Sembrar(Datos.Generico());
        var antes = repo.Todos.ToArray();
        await Sembrar(repo, false);
        Assert.Equal(antes, repo.Todos);
        Assert.Equal("CLIENTE EXISTENTE", normal.RazonSocial);
        Assert.Null(normal.FechaActualizacion);
        Assert.Equal(genericoExistente, repo.Todos.Any(c => c.EsGenerico));
    }

    private static Task Sembrar(RepositorioEnMemoria repo, bool incluirGenerico) =>
        ClientesDatabaseInitializer.SembrarSiEstaVaciaAsync(repo, repo.Todos.Count, incluirGenerico,
            Datos.Ahora, NullLogger.Instance, CancellationToken.None);
}
