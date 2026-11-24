using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Proveedores.Infrastructure.Mongo;

public sealed class MongoStartupInitializer(
    IMongoCollection<ProveedorDocument> coleccion,
    IOptions<MongoSettings> opciones,
    ILogger<MongoStartupInitializer> logger) : IHostedService
{
    private const int MaximoIntentos = 10;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = opciones.Value;
        if (!settings.InitializeOnStartup)
        {
            logger.LogInformation("Inicialización de MongoDB desactivada (Mongo:InitializeOnStartup=false).");
            return;
        }

        for (var intento = 1; ; intento++)
        {
            try
            {
                await ProveedorIndexes.CrearAsync(coleccion, cancellationToken);
                if (settings.SeedEnabled)
                    await ProveedorSeeder.SembrarAsync(coleccion, logger, cancellationToken);
                return;
            }
            catch (Exception ex) when (intento < MaximoIntentos && ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "MongoDB aún no está listo (intento {Intento}/{Max}). Reintentando...", intento, MaximoIntentos);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
