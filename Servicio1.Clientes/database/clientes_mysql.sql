-- Base de datos propia del microservicio Clientes (MySQL 8.4). Sin FK a otros servicios.
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
