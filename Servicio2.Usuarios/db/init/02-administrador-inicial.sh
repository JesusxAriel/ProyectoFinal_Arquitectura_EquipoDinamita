#!/bin/sh
set -eu

if [ -z "${PUNTOMARKET_ADMIN_USERNAME:-}" ] || [ -z "${PUNTOMARKET_ADMIN_EMAIL:-}" ]; then
    echo "No se siembra el administrador inicial: faltan PUNTOMARKET_ADMIN_USERNAME y PUNTOMARKET_ADMIN_EMAIL."
    exit 0
fi

NOMBRES="${PUNTOMARKET_ADMIN_NOMBRE:-Administrador}"
APELLIDO_PATERNO="PuntoMarket"

if ! echo "$PUNTOMARKET_ADMIN_USERNAME" | grep -Eq '^[A-Za-z0-9._-]{3,50}$'; then
    echo "PUNTOMARKET_ADMIN_USERNAME solo admite letras, numeros, punto, guion y guion bajo."
    exit 1
fi

if ! echo "$PUNTOMARKET_ADMIN_EMAIL" | grep -Eq "^[^@[:space:]'\"\\\\]+@[^@[:space:]'\"\\\\]+\\.[^@[:space:]'\"\\\\]+\$"; then
    echo "PUNTOMARKET_ADMIN_EMAIL no tiene la forma de un correo valido o usa comillas o barras invertidas."
    exit 1
fi

if ! echo "$NOMBRES" | grep -Eq "^[^'\\\\]{1,100}$"; then
    echo "PUNTOMARKET_ADMIN_NOMBRE no admite comillas simples ni barras invertidas."
    exit 1
fi

mysql --protocol=socket --user=root --password="$MYSQL_ROOT_PASSWORD" --database="$MYSQL_DATABASE" <<SQL
INSERT INTO USUARIO
    (username, correo, nombres, apellidoPaterno, idRol, activo, debeCambiarPassword, usuarioCreacion, fechaCreacion)
SELECT '$PUNTOMARKET_ADMIN_USERNAME', '$PUNTOMARKET_ADMIN_EMAIL', '$NOMBRES', '$APELLIDO_PATERNO',
       id, 1, 0, '$PUNTOMARKET_ADMIN_USERNAME', UTC_TIMESTAMP()
FROM ROL
WHERE nombre = 'Administrador' AND estaActivo = 1
ON DUPLICATE KEY UPDATE activo = 1;
SQL
