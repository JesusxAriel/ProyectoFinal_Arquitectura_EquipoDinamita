SET @tiene_nombres = (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'USUARIO'
      AND COLUMN_NAME = 'nombres'
);

SET @sentencia = IF(
    @tiene_nombres = 0,
    "ALTER TABLE USUARIO
        ADD COLUMN nombres VARCHAR(100) NULL AFTER correo,
        ADD COLUMN apellidoPaterno VARCHAR(60) NULL AFTER nombres,
        ADD COLUMN apellidoMaterno VARCHAR(60) NULL AFTER apellidoPaterno",
    "SELECT 1"
);

PREPARE aplicar_migracion FROM @sentencia;
EXECUTE aplicar_migracion;
DEALLOCATE PREPARE aplicar_migracion;

SET @tiene_completo = (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'USUARIO'
      AND COLUMN_NAME = 'nombreCompleto'
);

SET @sentencia = IF(
    @tiene_completo = 1,
    "UPDATE USUARIO u
        JOIN (
            SELECT id,
                   TRIM(REGEXP_REPLACE(nombreCompleto, '[[:space:]]+', ' ')) AS n,
                   CHAR_LENGTH(TRIM(REGEXP_REPLACE(nombreCompleto, '[[:space:]]+', ' ')))
                       - CHAR_LENGTH(REPLACE(TRIM(REGEXP_REPLACE(nombreCompleto, '[[:space:]]+', ' ')), ' ', '')) + 1 AS c
            FROM USUARIO
            WHERE nombres IS NULL
        ) t ON t.id = u.id
        SET u.nombres = LEFT(CASE
                WHEN t.c = 1 THEN t.n
                WHEN t.c <= 3 THEN SUBSTRING_INDEX(t.n, ' ', 1)
                ELSE SUBSTRING_INDEX(t.n, ' ', t.c - 2)
            END, 100),
            u.apellidoPaterno = LEFT(CASE
                WHEN t.c = 1 THEN 'PuntoMarket'
                WHEN t.c = 2 THEN SUBSTRING_INDEX(t.n, ' ', -1)
                ELSE SUBSTRING_INDEX(SUBSTRING_INDEX(t.n, ' ', -2), ' ', 1)
            END, 60),
            u.apellidoMaterno = CASE
                WHEN t.c >= 3 THEN LEFT(SUBSTRING_INDEX(t.n, ' ', -1), 60)
                ELSE NULL
            END",
    "SELECT 1"
);

PREPARE aplicar_migracion FROM @sentencia;
EXECUTE aplicar_migracion;
DEALLOCATE PREPARE aplicar_migracion;

SET @sentencia = IF(
    @tiene_completo = 1,
    "ALTER TABLE USUARIO
        MODIFY nombres VARCHAR(100) NOT NULL,
        MODIFY apellidoPaterno VARCHAR(60) NOT NULL,
        DROP COLUMN nombreCompleto",
    "SELECT 1"
);

PREPARE aplicar_migracion FROM @sentencia;
EXECUTE aplicar_migracion;
DEALLOCATE PREPARE aplicar_migracion;
