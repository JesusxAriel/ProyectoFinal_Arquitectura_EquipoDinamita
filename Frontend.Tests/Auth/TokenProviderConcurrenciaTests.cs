using System.Reflection;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Tests.Auth;

[CollectionDefinition(NombreColeccion, DisableParallelization = true)]
public sealed class TokenProviderSerieColeccion
{
    public const string NombreColeccion = "TokenProviderSerie";
}

[Collection(TokenProviderSerieColeccion.NombreColeccion)]
public sealed class TokenProviderConcurrenciaTests
{
    private const int Hilos = 50;

    private static readonly FieldInfo CampoInstancia =
        typeof(TokenProvider).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static async Task<TokenProvider[]> PedirInstanciaEnParalelo()
    {
        using var barrera = new Barrier(Hilos);

        var tareas = Enumerable.Range(0, Hilos)
            .Select(_ => Task.Factory.StartNew(
                () =>
                {
                    barrera.SignalAndWait();
                    return TokenProvider.Instance;
                },
                TaskCreationOptions.LongRunning))
            .ToArray();

        return await Task.WhenAll(tareas);
    }

    [Fact]
    public async Task Instance_ConMuchasTareasEnParalelo_DevuelveSiempreLaMismaReferencia()
    {
        var instancias = await PedirInstanciaEnParalelo();

        Assert.Equal(Hilos, instancias.Length);
        Assert.All(instancias, instancia => Assert.Same(instancias[0], instancia));
        Assert.Same(TokenProvider.Instance, instancias[0]);
    }

    [Fact]
    public async Task Instance_SinInstanciaPrevia_CreaUnaSolaAunqueTodasLasTareasLaPidanAlMismoTiempo()
    {
        Assert.NotNull(CampoInstancia);
        CampoInstancia.SetValue(null, null);

        var instancias = await PedirInstanciaEnParalelo();

        Assert.All(instancias, instancia => Assert.Same(instancias[0], instancia));
        Assert.Same(CampoInstancia.GetValue(null), instancias[0]);
    }

    [Fact]
    public void Instance_EsUnaInstanciaDeTokenProviderQueImplementaLaInterfaz()
    {
        ITokenProvider proveedor = TokenProvider.Instance;

        Assert.IsType<TokenProvider>(proveedor);
    }
}
