CREATE TABLE IF NOT EXISTS ROL (
    id INT NOT NULL AUTO_INCREMENT,
    nombre VARCHAR(50) NOT NULL,
    estaActivo TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_rol_nombre (nombre)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS USUARIO (
    id INT NOT NULL AUTO_INCREMENT,
    username VARCHAR(50) NOT NULL,
    correo VARCHAR(150) NOT NULL,
    nombreCompleto VARCHAR(150) NOT NULL,
    idRol INT NOT NULL,
    activo TINYINT(1) NOT NULL DEFAULT 1,
    debeCambiarPassword TINYINT(1) NOT NULL DEFAULT 1,
    usuarioCreacion VARCHAR(50) NOT NULL,
    fechaCreacion DATETIME NOT NULL,
    usuarioModificacion VARCHAR(50) NULL,
    fechaModificacion DATETIME NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_usuario_username (username),
    UNIQUE KEY uq_usuario_correo (correo),
    CONSTRAINT fk_usuario_rol FOREIGN KEY (idRol) REFERENCES ROL (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

INSERT INTO ROL (nombre, estaActivo) VALUES ('Administrador', 1), ('Cajero', 1)
    ON DUPLICATE KEY UPDATE estaActivo = 1;
