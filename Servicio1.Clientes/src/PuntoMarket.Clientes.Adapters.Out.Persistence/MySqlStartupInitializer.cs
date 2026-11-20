using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using PuntoMarket.Clientes.Domain.Creators;
using PuntoMarket.Clientes.Domain.Entities;

namespace PuntoMarket.Clientes.Adapters.Out.Persistence;

public sealed class MySqlStartupInitializer(MySqlDataSource dataSource, ILogger<MySqlStartupInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var c = await dataSource.OpenConnectionAsync(ct);
        await using var create = c.CreateCommand();
        create.CommandText = """
CREATE TABLE IF NOT EXISTS CLIENTE (
 id INT AUTO_INCREMENT PRIMARY KEY,
 nitCi VARCHAR(30) NOT NULL,
 razonSocial VARCHAR(150) NOT NULL,
 correo VARCHAR(150) NULL,
 telefono VARCHAR(30) NULL,
 estaActivo TINYINT(1) NOT NULL DEFAULT 1 CHECK (estaActivo IN (0,1)),
 idUsuarioCreacion VARCHAR(64) NOT NULL,
 idUsuarioActualizacion VARCHAR(64) NULL,
 fechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 fechaActualizacion DATETIME NULL,
 nitCiActivo VARCHAR(30) GENERATED ALWAYS AS (IF(estaActivo=1,nitCi,NULL)) VIRTUAL,
 UNIQUE KEY UQ_Cliente_nitCi_activo(nitCiActivo),
 KEY IX_Cliente_razonSocial(razonSocial)
) ENGINE=InnoDB CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
""";
        await create.ExecuteNonQueryAsync(ct);
        var seed = new[] {
            ("0", "Sin Nombre"),
            ("12345678", "Cliente Pérez"),
            ("87654321", "Comercial Ejemplo")
        };
        foreach (var (nit, razon) in seed)
        {
            await using var insert = c.CreateCommand();
            insert.CommandText = "INSERT IGNORE INTO CLIENTE(nitCi,razonSocial,correo,telefono,estaActivo,idUsuarioCreacion,fechaCreacion) VALUES(@nit,@razon,NULL,NULL,1,'sistema-seed',UTC_TIMESTAMP())";
            insert.Parameters.AddWithValue("@nit", nit);
            insert.Parameters.AddWithValue("@razon", razon);
            await insert.ExecuteNonQueryAsync(ct);
        }
        logger.LogInformation("Se creó/verificó la tabla CLIENTE y se sembraron los clientes iniciales.");
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
