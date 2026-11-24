# Activación de cuentas por enlace

Este documento describe el contrato del backend de Usuarios para que el Frontend porte la pantalla de activación.

## Flujo

1. El administrador crea un usuario (`POST /api/usuarios`). El servicio lo registra en Keycloak con una contraseña aleatoria que nadie conoce, guarda el hash de un token de un solo uso y envía por correo el enlace de activación. La respuesta es `201` con `{ id, username, correoEnviado }`; si el correo falla, igual responde `201` con `correoEnviado=false`.
2. El usuario abre `ACTIVACION_BASE_URL/Auth/Activar?token=<token>` y define su contraseña dos veces.
3. El Frontend envía el token y las contraseñas a `POST /api/activacion` por el Gateway. Al activarse, el token deja de servir y el usuario inicia sesión con la contraseña elegida.
4. Si el usuario pierde el enlace o este vence, el administrador usa `POST /api/usuarios/{id}/reenviar-acceso`.

## Token

- 32 bytes aleatorios criptográficos, en base64url sin relleno (43 caracteres).
- En la base solo se guarda el hash SHA-256 en hexadecimal (`tokenActivacionHash`) y el vencimiento en UTC (`tokenActivacionExpira`). El valor original solo existe en el correo.
- Vigencia configurable con `Activacion:VigenciaMinutos` (variable `ACTIVACION_VIGENCIA_MINUTOS`). Por defecto 60 minutos; acepta de 1 a 10080.
- Uso único: la activación lo anula de forma atómica. Un usuario tiene como máximo un token vigente, de modo que reenviar el acceso invalida los enlaces anteriores.
- Un usuario inactivo no puede activar con un token antiguo. Al desactivar a un usuario se anula su token.
- El token nunca se escribe en logs ni aparece en mensajes de error. Viaja en el cuerpo del POST, no en la URL del servicio.

## Activar la cuenta

`POST /api/activacion`, anónimo. El Gateway lo expone en la ruta `usuarios-activacion-route`, solo con el método POST y sin exigir JWT. El resto de `/api/usuarios/**` sigue exigiéndolo.

Cuerpo (`application/json`):

```json
{
  "token": "valor-del-enlace",
  "nuevaContrasena": "NuevaClave#2026",
  "confirmarContrasena": "NuevaClave#2026"
}
```

| Estado | Cuerpo | Cuándo |
|---|---|---|
| `200` | `{ "activada": true }` | Contraseña fijada en Keycloak y cuenta activada (`debeCambiarPassword=false`) |
| `400` | `{ "error": "El enlace de activación no es válido o ha vencido. Solicite uno nuevo al administrador." }` | Token inexistente, vencido, ya usado, vacío, de un usuario inactivo o cuerpo ausente. Todos los casos devuelven exactamente este mensaje |
| `400` | `{ "error": "La confirmación no coincide con la contraseña nueva." }` | Las contraseñas no coinciden |
| `400` | `{ "error": "La contraseña debe tener al menos 8 caracteres." }` y variantes | La contraseña incumple la política |
| `503` | `{ "error": "No se pudo completar la activación. Intente nuevamente más tarde." }` | Keycloak o la base no respondieron. El token se restaura y el mismo enlace puede reutilizarse |

La política exige mínimo 8 caracteres, mayúscula, minúscula, número y un carácter especial. La confirmación y la política se validan antes de consumir el token, por lo que un error de contraseña no gasta el enlace ni revela si el token es válido.

Para mostrar el motivo, el Frontend puede usar directamente el campo `error`. Un `400` por política o confirmación no consume el enlace: el usuario puede corregir y reintentar. Un `400` con el mensaje de enlace inválido es definitivo: debe pedir uno nuevo.

## Reenviar acceso

`POST /api/usuarios/{id}/reenviar-acceso` (solo Administrador). El contrato no cambia: `200 { "correoEnviado": true|false }`.

- `404 { "error" }` si el usuario no existe; `400 { "error" }` si está inactivo.
- Fija en Keycloak una contraseña aleatoria nueva (la contraseña vigente deja de funcionar), deja `debeCambiarPassword=true`, genera un token nuevo que invalida los anteriores y envía el correo.
- Si el correo falla, responde `200` con `correoEnviado=false`; el administrador puede repetir la operación.
- La activación posterior deja `debeCambiarPassword=false`, igual que en el alta.

## Correo

Asunto: "Active su cuenta de PuntoMarket". El cuerpo trata de usted, incluye el nombre de usuario y el enlace, indica que se puede usar una sola vez y cuánto tarda en vencer, y no contiene ninguna contraseña.

El enlace es `ACTIVACION_BASE_URL/Auth/Activar?token=...`. `ACTIVACION_BASE_URL` debe ser una dirección HTTP o HTTPS sin credenciales, parámetros ni fragmento (por defecto `http://localhost:5100`, el puerto del Frontend en el Compose). Con una URL inválida el envío falla con un mensaje claro y el alta responde `correoEnviado=false`.

## Esquema

Columnas nuevas en `USUARIO`, definidas en `db/init/01-esquema-usuarios.sql`:

| Columna | Tipo | Uso |
|---|---|---|
| `tokenActivacionHash` | `CHAR(64) NULL`, índice único | Hash SHA-256 del token vigente |
| `tokenActivacionExpira` | `DATETIME NULL` | Vencimiento en UTC |

Las bases creadas antes de este cambio necesitan aplicar una sola vez `db/migraciones/001-activacion-por-enlace.sql`, que es idempotente y no se ejecuta automáticamente:

```powershell
Get-Content Servicio2.Usuarios/db/migraciones/001-activacion-por-enlace.sql -Raw | docker compose exec -T usuarios-db sh -c 'mysql -uroot -p"$MYSQL_ROOT_PASSWORD" "$MYSQL_DATABASE"'
```

## Guía para la pantalla `/Auth/Activar` del Frontend

- La ruta debe ser anónima. El prefijo `/Auth` ya está excluido de la sesión obligatoria.
- Patrón recomendado: el GET con `?token=` guarda el token en memoria del servidor, entrega al navegador solo una referencia aleatoria en una cookie `HttpOnly` y redirige a la misma ruta sin consulta, para que el token no quede en el historial, en el formulario ni en cabeceras `Referer`. El POST usa antiforgery.
- Cliente tipado sin JWT hacia `POST api/activacion` del Gateway, sin reintentos automáticos. Debe mostrar el mensaje `error` del servidor y, tras un `200`, enlazar a `/Auth/Login`.
- Responder con `Cache-Control: no-store` y `Referrer-Policy: no-referrer`. No habilitar el registro de consultas en proxies para esta ruta, porque el enlace original sí lleva el token.
- Las contraseñas no deben repoblarse en el formulario.
