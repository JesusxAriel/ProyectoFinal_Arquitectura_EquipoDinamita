# Activación de cuentas en el Frontend

`/Auth/Activar` es una ruta pública: el prefijo `/Auth` está exento de la sesión obligatoria. El contrato del backend está en [Servicio2.Usuarios/ACTIVACION.md](../Servicio2.Usuarios/ACTIVACION.md).

## Flujo

1. El correo lleva `ACTIVACION_BASE_URL/Auth/Activar?token=...`. Un GET con el token no activa nada: `ActivarModel` guarda el token en `IMemoryCache` mediante `ActivacionesPendientes`, entrega al navegador solo una referencia aleatoria de 64 caracteres hexadecimales y redirige a `/Auth/Activar` sin consulta, de modo que el token no queda en la barra de direcciones, en el formulario ni en cabeceras `Referer`.
2. El GET sin consulta muestra el formulario (nueva contraseña y confirmación) si la referencia sigue vigente.
3. El POST usa antiforgery, lee las contraseñas directamente del formulario y las envía al Gateway con `ActivacionApiAdapter`. Las contraseñas nunca se repueblan en el HTML.
4. Con un 200 el Frontend borra la referencia y la cookie y redirige a `/Auth/Login`, que muestra una sola vez "Su cuenta fue activada. Ya puede iniciar sesión." (`TempData`, con un `alert-success`). No se inicia sesión automáticamente.

## Referencia en el servidor y cookie

- Cookie `PuntoMarket.Activacion`: `HttpOnly`, `SameSite=Lax`, `Path=/Auth/Activar` (más el `PathBase`), `Secure` cuando la petición es HTTPS y `Max-Age` de 60 minutos.
- `SameSite=Lax` es necesario porque el enlace llega desde el correo (navegación entre sitios): con `Strict` el navegador omitiría la cookie en el GET de la redirección. El POST sigue protegido por antiforgery.
- La referencia dura 60 minutos, igual que la vigencia predeterminada del enlace (`Activacion:VigenciaMinutos`). Si el administrador cambia esa vigencia en el backend, la referencia sigue siendo de 60 minutos como máximo: el backend decide siempre la validez real del token.
- El estado vive en la memoria del proceso. Reiniciar el Frontend invalida las referencias; el usuario puede volver a abrir el enlace del correo si todavía no venció. Con varias réplicas hace falta afinidad de sesión (igual que con las claves de antiforgery).

## Mensajes

| Situación | Mensaje | Formulario | Estado |
|---|---|---|---|
| Sin token, token vacío o malformado | `MensajesActivacion.EnlaceInvalido` | Oculto, con enlace al login | 400 |
| La referencia venció o se perdió | "Vuelva a abrir el enlace que recibió por correo." | Oculto, con enlace al login | 400 |
| El backend responde 400 o 503 con `error` | El mensaje del backend, tal cual | Visible, se puede reintentar | 400 |
| El backend responde 400 con el mensaje de enlace inválido | El mensaje del backend | Oculto, con enlace al login; se borra la referencia | 400 |
| Campos vacíos | "Ingrese la nueva contraseña." o "Confirme la nueva contraseña." | Visible | 400 |
| Sin respuesta del backend | "No se pudo conectar con el servidor. Intente nuevamente en unos minutos." | Visible | 400 |

La política de contraseña y la confirmación las valida el backend, que devuelve el mensaje específico sin consumir el enlace. El Frontend solo comprueba que los campos no estén vacíos para no duplicar reglas.

## Dependencia con el backend

`MensajesActivacion.EnlaceInvalido` debe ser idéntico, carácter por carácter, a `ActivarCuentaCasoDeUso.EnlaceInvalido` de Usuarios. La página lo compara con el `error` de la respuesta 400 para decidir si el enlace ya no sirve (oculta el formulario y elimina la referencia) o si el usuario puede corregir la contraseña y reintentar. El test `El_mensaje_de_enlace_invalido_del_Frontend_es_identico_al_del_backend` falla si alguno de los dos textos cambia sin el otro; por eso `Frontend.Tests` referencia el proyecto `PuntoMarket.Usuarios.Application`.

## Cabeceras y registros

- Todas las respuestas de la página llevan `Cache-Control: no-store` y `Referrer-Policy: no-referrer`.
- `appsettings.json` fija `Microsoft.AspNetCore` en `Warning`, por lo que los mensajes informativos de `Hosting.Diagnostics`, que incluyen la consulta de la petición inicial, no se escriben. No habilite el registro de consultas ni de cuerpos en el Frontend. Un proxy, un balanceador o una herramienta de monitoreo externa debe omitir o redactar la consulta de `/Auth/Activar`: el Frontend no controla esos registros. No copie el enlace original a terminales ni a incidencias.

## Cliente HTTP

`ActivacionApiAdapter` llama a `POST api/activacion` por el Gateway sin JWT (no usa `BearerTokenHandler`), sin reintentos y sin seguir redirecciones. Se registra con `AddActivacionDeCuentas()`, que usa `GatewayApi:BaseUrl`.

## Configuración

`ACTIVACION_BASE_URL` es la dirección pública del Frontend y la define el servicio de Usuarios (variable de Compose). Debe coincidir con la dirección con la que los usuarios abren el Frontend; el valor predeterminado `http://localhost:5100` corresponde al puerto publicado por Compose.
