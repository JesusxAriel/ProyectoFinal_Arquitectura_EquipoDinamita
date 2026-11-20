namespace PuntoMarket.Clientes.Adapters.Out.Persistence.Initialization;

/// <summary>
/// Script de creación de la tabla CLIENTE (copia en Servicio1.Clientes/database/clientes_mysql.sql para el informe).
/// No hay clave foránea hacia USUARIO: esa tabla pertenece a otro microservicio con su propia base.
/// idUsuarioCreacion / idUsuarioActualizacion guardan el claim "sub" del JWT (texto, p. ej. UUID de Keycloak).
/// La unicidad del NIT/CI solo entre clientes ACTIVOS se logra con una columna virtual (nitCiActivo) y un índice único:
/// al dar de baja un cliente, nitCiActivo pasa a NULL y el NIT queda libre.
/// </summary>
internal static class ClientesSql
{
    public const string CrearTablaCliente = """
        CREATE TABLE IF NOT EXISTS `CLIENTE` (
          `id` INT NOT NULL AUTO_INCREMENT,
          `nitCi` VARCHAR(30) NOT NULL,
          `razonSocial` VARCHAR(150) NOT NULL,
          `correo` VARCHAR(150) DEFAULT NULL,
          `telefono` VARCHAR(30) DEFAULT NULL,
          `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
          `idUsuarioCreacion` VARCHAR(64) NOT NULL,
          `idUsuarioActualizacion` VARCHAR(64) DEFAULT NULL,
          `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
          `fechaActualizacion` DATETIME DEFAULT NULL,
          `nitCiActivo` VARCHAR(30) GENERATED ALWAYS AS (IF(`estaActivo` = 1, `nitCi`, NULL)) VIRTUAL,
          PRIMARY KEY (`id`),
          UNIQUE KEY `UQ_Cliente_nitCi_activo` (`nitCiActivo`),
          KEY `IX_Cliente_razonSocial` (`razonSocial`),
          CONSTRAINT `CK_Cliente_estaActivo` CHECK (`estaActivo` IN (0,1))
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
        """;
}
