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
