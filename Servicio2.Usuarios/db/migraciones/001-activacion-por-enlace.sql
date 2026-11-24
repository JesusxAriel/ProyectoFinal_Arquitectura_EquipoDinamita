SET @columnas_existentes = (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'USUARIO'
      AND COLUMN_NAME IN ('tokenActivacionHash', 'tokenActivacionExpira')
);

SET @sentencia = IF(
    @columnas_existentes = 0,
    'ALTER TABLE USUARIO
        ADD COLUMN tokenActivacionHash CHAR(64) NULL,
        ADD COLUMN tokenActivacionExpira DATETIME NULL,
        ADD UNIQUE KEY uq_usuario_token_activacion (tokenActivacionHash)',
    'SELECT 1'
);

PREPARE aplicar_migracion FROM @sentencia;
EXECUTE aplicar_migracion;
DEALLOCATE PREPARE aplicar_migracion;
