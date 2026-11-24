using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Creators;
using PuntoMarket.Clientes.Domain.Entities;

namespace PuntoMarket.Clientes.Adapters.Out.Persistence.Initialization;

/// <summary>
/// Prepara la base propia de Clientes al arrancar: crea la tabla (idempotente) y, si está vacía, siembra
/// opcionalmente el cliente genérico y dos clientes de ejemplo a través del dominio (creadores + validaciones).
/// Nunca sobrescribe ni borra datos existentes.
/// </summary>
public sealed class ClientesDatabaseInitializer(
    MySqlDataSource dataSource,
    IServiceScopeFactory scopeFactory,
    IOptions<ClientesDbOptions> opciones,
    TimeProvider reloj,
    ILogger<ClientesDatabaseInitializer> logger) : IHostedService
{
    private const string UsuarioSistema = "sistema";
    private const int IntentosMaximos = 10;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!opciones.Value.InitializeOnStartup) return;

        for (var intento = 1; ; intento++)
        {
            try
            {
                await InicializarAsync(cancellationToken);
                return;
            }
            catch (MySqlException ex) when (intento < IntentosMaximos)
            {
                logger.LogWarning(ex, "MySQL aún no responde (intento {Intento}/{Maximo}). Reintentando.", intento, IntentosMaximos);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task InicializarAsync(CancellationToken ct)
    {
        await using (var conexion = await dataSource.OpenConnectionAsync(ct))
        await using (var crear = new MySqlCommand(ClientesSql.CrearTablaCliente, conexion))
        {
            await crear.ExecuteNonQueryAsync(ct);
        }

        if (opciones.Value.SeedEnabled) await SembrarSiEstaVaciaAsync(ct);
    }

    private async Task SembrarSiEstaVaciaAsync(CancellationToken ct)
    {
        long cantidad;
        await using (var conexion = await dataSource.OpenConnectionAsync(ct))
        await using (var conteo = new MySqlCommand("SELECT COUNT(*) FROM CLIENTE", conexion))
        {
            cantidad = Convert.ToInt64(await conteo.ExecuteScalarAsync(ct));
        }

        using var scope = scopeFactory.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<IClienteRepository>();
        await SembrarSiEstaVaciaAsync(repositorio, cantidad, opciones.Value.SeedGenericoEnabled,
            reloj.GetUtcNow().UtcDateTime, logger, ct);
    }

    // La decisión y las inserciones se prueban sin abrir conexiones ni tocar datos persistentes.
    internal static async Task SembrarSiEstaVaciaAsync(IClienteRepository repositorio, long cantidad,
        bool incluirGenerico, DateTime ahora, ILogger logger, CancellationToken ct)
    {
        if (cantidad > 0) return;

        // Se conserva el orden histórico cuando el genérico está habilitado.
        var datos = new (TipoCliente Tipo, DatosCliente Datos)[]
        {
            (TipoCliente.Generico, ClienteGenericoCreator.DatosPorDefecto),
            (TipoCliente.Estandar, new DatosCliente("4545451226", "JUAN PÉREZ VARGAS", "jperez@gmail.com", "77123456")),
            (TipoCliente.Estandar, new DatosCliente("1020304050", "MARÍA ROJAS LÓPEZ", "mrojas@gmail.com", "70711223"))
        };

        foreach (var (tipo, dato) in datos)
        {
            if (tipo == TipoCliente.Generico && !incluirGenerico) continue;
            var creacion = ClienteCreatorSelector.Para(tipo).Crear(dato, UsuarioSistema, ahora);
            if (creacion.IsFailure)
            {
                logger.LogError("Dato de ejemplo inválido ({Codigo}): {Mensaje}", creacion.Error.Code, creacion.Error.Message);
                continue;
            }

            var guardado = await repositorio.AgregarAsync(creacion.Value, ct);
            if (guardado.IsFailure)
                logger.LogWarning("No se sembró {NitCi}: {Codigo}", dato.NitCi, guardado.Error.Code);
        }

        logger.LogInformation("Tabla CLIENTE vacía: se sembraron los clientes iniciales.");
    }
}
