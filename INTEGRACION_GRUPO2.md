# Estado de la integración local — micro-proveedor

Se integran las fuentes de origin/main (3eefb83) y los cambios propios de Proveedores (51b63b4). `docker-compose.yml` es el entorno común original; `docker-compose.demo.yml` conserva la demo independiente. La solución raíz cubre 25 proyectos. No se han arrancado servicios comunes ni cambiado bases o volúmenes durante esta fase.

## Acuerdos vigentes

- Servicio4.Proveedores es definitivo; conserva los cinco proyectos, contratos, JWT, validaciones, auditoría y concurrencia.
- Gateway/ y Frontend/ comunes son la referencia; la UI de Proveedores permanece en Frontend/Frontend.Demo y no se ha trasplantado ni sustituye al frontend común.
- Grupo 2 debe integrar las páginas y cliente HTTP de Proveedores con AddGatewayAdapter y BearerTokenHandler, sesión por usuario, rutas/layout y roles reales, sin reutilizar DemoTokenProvider ni DemoPerfilSession como identidad definitiva.
- Gateway común exige TokenValido para Proveedores y SoloAdministrador para Usuarios, con excepciones concretas de usuario actual. La API Proveedores conserva autorización propia Administrador/Cajero.
- Keycloak usa realm puntomarket, cliente puntomarket-web y mappers de audiencia para las cuatro APIs. Proveedores requiere proveedores-api. El issuer interno del Compose debe coincidir con el iss real; no desactivar validación para resolver DNS o un 401. MetadataAddress sigue disponible para discovery separado si se acuerda un issuer público.
- La demo utiliza DevIdentity y el realm proveedores-dev; los tokens de ambos entornos no son intercambiables. No duplicar autenticación común en el demo.
- La configuración Mongo común se conserva literalmente: proveedores-db autenticado, base tomada de PROVEEDORES_DB_NAME y volumen proveedores-db-data. La demo mantiene mongodb, proveedores_db y mongodb_data. Mongo Windows en 27017 es un tercer servidor independiente. No migrar, compartir o renombrar bases/volúmenes automáticamente.
- Puertos demo predeterminados 15100/15000/15200/15300/27028; los comunes conservan sus valores de main. Revisar variables locales que puedan sobrescribirlos.
- La suite propia usa bases temporales; las pruebas comunes y builds no acreditan un login real o conectividad SMTP/MySQL/Keycloak del conjunto.
- No hay cambios en contratos públicos durante esta integración. La regla de productos activos y la versión de cliente/ETag continúan pendientes.

## Documentación común conservada

El contenido siguiente procede de main. Sus notas de entregas anteriores describen el contexto de esas entregas; para ubicación definitiva y separación de entornos prevalecen los acuerdos de arriba y el README actual.

# Integración con el Grupo 2 (Keycloak, Gateway y frontend definitivos)

## 1. Arquitectura
```mermaid
flowchart LR
  F[Frontend Razor] -->|Bearer JWT| G[API Gateway YARP]
  G -->|/api/proveedores| P[Proveedores.API]
  P --> M[(MongoDB)]
  P -. valida JWT por JWKS .-> K[Keycloak / DevIdentity]
  F -. token .-> K
  P -. futuro .-> PR[API Productos]
```
El Gateway reenvía `Authorization`; **la API valida** firma, issuer, audience y expiración, y comprueba roles.

## 2-3. Definitivo vs temporal
| Definitivo | Temporal |
|---|---|
| `Servicio4.Proveedores/*`, MongoDB | `ApiGateway/ApiGateway.Demo`, `Frontend/Frontend.Demo`, `DevIdentity/DevIdentity.Demo` |

## 4-5. Archivos
| Archivo | Estado | Responsable | Qué modificar | Cómo verificar |
|---|---|---|---|---|
| `Servicio4.Proveedores/Proveedores.API/appsettings.json` (`Authentication:Jwt`) | Preparado | Grupo 2 | **Obligatorio:** Authority, Audience, RoleClaimPaths, RoleMapping | `GET /api/proveedores` con token real → 200 |
| `Proveedores.API/Security/KeycloakRolesClaimsMapper.cs` | Preparado | Grupo 2 | Opcional: solo si la estructura de claims difiere | `KeycloakRolesClaimsMapperTests` |
| `Proveedores.API/Security/ConfigureJwtBearerOptions.cs` | Conservar | Grupo 1 | Nada | 401 sin token / token inválido |
| `Proveedores.API/Security/HttpContextCurrentUser.cs` | Preparado | Grupo 2 | Opcional: `UserIdClaim` | `idUsuarioAdmin` = `sub` del token |
| `ApiGateway/ApiGateway.Demo/appsettings.json` | Temporal | Grupo 2 | Agregar rutas/clusters; quitar `dev-auth-route` | `curl` al Gateway |
| `Frontend/Frontend.Demo/Services/DemoPerfilSession.cs`, `DemoTokenProvider.cs` | Temporal | Grupo 2 | **Obligatorio al integrar:** eliminar y usar el token del usuario real | Login real → llamadas con su token |
| `Frontend/Frontend.Demo/Services/ProveedoresApiClient.cs` | Conservar | Grupo 2 | Sustituir el origen del token (`DemoTokenProvider`) | CRUD desde UI |
| `DevIdentity/DevIdentity.Demo/*`, servicio `dev-identity` en `docker-compose.yml` | Temporal | Grupo 2 | Eliminar | La API sigue validando contra Keycloak |
| Docker y variables de despliegue | Pendiente | Ambos | Se incorporarán en un commit independiente | Pendiente de entrega Docker |
| `Proveedores.Infrastructure/Integrations/ProductoApiAdapter.cs` | Pendiente (futuro) | Futura entrega | Activar `ProductosApi__*` y usar el puerto | `ProductoApiAdapterTests` |

**Cambios obligatorios para Keycloak:** Authority, Audience, RoleMapping/RoleClaimPaths, quitar identidad temporal. **Opcionales:** UserIdClaim, rutas del Gateway. **Pendientes de futuras entregas:** integración con Productos (§19).

## 6-7. Keycloak, Authority y Audience
1. Cliente/audiencia `proveedores-api` en el realm; agregue un *Audience mapper* para que el token incluya `aud=proveedores-api`.
2. `Authentication__Jwt__Authority=https://<host>/realms/<realm>` (la API descubre `/.well-known/openid-configuration` y el JWKS).
3. `Authentication__Jwt__RequireHttpsMetadata=true` en producción. El `iss` del token debe ser idéntico a Authority.

## 8. Roles
Roles internos: `Administrador`, `Cajero`. Mapeo por configuración:
```
Authentication__Jwt__RoleClaimPaths__0=realm_access.roles
Authentication__Jwt__RoleClaimPaths__1=resource_access.proveedores-api.roles
Authentication__Jwt__RoleMapping__Administrador__0=admin
Authentication__Jwt__RoleMapping__Cajero__0=cajero
```
Permisos: lectura = Administrador o Cajero; escritura = solo Administrador.

## 9. Claim `sub`
La auditoría guarda el `sub` (cadena) en `idUsuarioAdmin` / `idUsuarioActualizacion`. No se asume entero.

## 10-11. Gateway y frontend definitivo
Agregue rutas en `ReverseProxy` del Gateway. El frontend definitivo debe enviar `Authorization: Bearer <JWT>` a `{Gateway}/api/proveedores` y mostrar los mensajes `detail` de los ProblemDetails (código `code` estable).

## 12-13. Ejemplos
```http
GET /api/proveedores?busqueda=demo&estado=Activos&ordenarPor=Nombre&descendente=false&pagina=1&tamanoPagina=10
Authorization: Bearer <token>            → 200 { items, pagina, tamanoPagina, total, totalPaginas }

POST /api/proveedores   {"nombreEmpresa":"Lácteos Sur","numeroEmpresa":"71234567","correoReferencia":"a@b.com","esAutogestionado":false}
                                         → 201 + Location | 400 datos inválidos | 409 nombre activo duplicado
PUT /api/proveedores/{id}                → 200 | 404 | 409 (también si está desactivado)
DELETE /api/proveedores/{id}             → 204 | 404 | 409 (ya desactivado)
PATCH /api/proveedores/{id}/reactivar    → 200 | 404 | 409 (ya activo o nombre tomado)
Sin token → 401 · Cajero en escritura → 403
```

## 14-15. Variables y Docker
Docker y sus archivos no se incluyen en esta publicación inicial. Utilizar el arranque local del README; las variables Docker se acordarán en una entrega posterior.

## 16. Pruebas de integración
`scripts/smoke-test.sh` (Gateway) y `dotnet test` (ver README). Con Keycloak real: obtener token del realm y repetir los casos 401/403/200.

## 17. Errores frecuentes
| Síntoma | Causa / solución |
|---|---|
| 401 con token válido | `iss` ≠ Authority, `aud` ≠ Audience o reloj desfasado |
| 403 siendo admin | Rol no mapeado: revise RoleClaimPaths/RoleMapping y decodifique el token |
| La API no arranca por metadatos | Authority inalcanzable desde el contenedor (use el nombre de servicio Docker) |
| 409 al reactivar | Existe otro activo con el mismo nombre |
| Gateway 502/503 | Dirección del cluster incorrecta (en Docker usar `http://proveedores-api:8080/`) |

## 18. Retirar la autenticación temporal
1. Quite `dev-identity` de `docker-compose.yml` y el `depends_on` que lo menciona.
2. Quite `dev-auth-route` y `dev-identity-cluster` del Gateway.
3. Elimine `DevIdentity/` y, en el frontend, `DemoPerfilSession`/`DemoTokenProvider` y el selector de perfil.
4. Configure Authority/Audience de Keycloak. Domain, Application y el CRUD no cambian.

## 19. Conectar la API de Productos (futuro)
Contrato **propuesto** (a acordar): `GET api/productos/proveedor/{proveedorId}/activos/conteo` → `200 {"cantidad": n}`.
1. `ProductosApi__Enabled=true`, `ProductosApi__BaseUrl=http://productos-api:8080/`.
2. Inyectar `IProductoApiClient` en `DesactivarProveedor`; si devuelve `true` → error Conflict; si devuelve Failure → propagar (503), nunca asumir `false`.
3. Agregar pruebas con handler falso (ver `ProductoApiAdapterTests`).

## 20. Correcciones de estabilidad local (18 de noviembre de 2026)
- PUT, DELETE y PATCH pueden devolver 409 con `code=Proveedor.CambioConcurrente` cuando otra solicitud modificó el documento entre lectura y escritura. El cliente debe recargar antes de volver a intentar. No se modifica la forma de los DTOs ni se exige enviar una versión desde el cliente; este control protege operaciones solapadas en el servidor, no formularios antiguos enviados después de una operación ya completada.
- MongoDB almacena `version` (Int64), incrementada de forma atómica junto con cada actualización. Documentos sin campo equivalen a 0 y se actualizan sin migración masiva. Todos los escritores deben usar este repositorio; una edición manual de MongoDB debe respetar la versión.
- El frontend temporal renueva solo ante 401, manteniendo el perfil y reconstruyendo cuerpo y encabezados. Reintenta una vez; no repite 403, fallos técnicos ni respuestas de negocio. La renovación se serializa por perfil y una respuesta tardía no elimina un token ya renovado. Un segundo 401 se muestra al usuario y también invalida su token. La caché JWKS de la API puede demorar el reconocimiento de una clave recién generada.
- Parámetros extremos de paginación se normalizan para mantener `(pagina-1)*tamanoPagina` dentro de Int32; se devuelve la página normalizada. `totalPaginas` se satura en Int32.MaxValue para conservar el contrato numérico existente.
- El contrato propuesto de Productos requiere `cantidad` presente, de tipo entero de 32 bits y >= 0. Campos ausentes, nulos, cadenas, fracciones y valores fuera de rango devuelven Failure de integración; no se asumen cero productos. No se ha activado la integración ni Keycloak real.
- Sin cambios de puertos, variables de entorno, claims ni rutas. Docker Compose no se ha modificado.

## 21. Frontend PuntoMarket (demostración)
- Nuevas páginas de frontend: `/Login`, `/Logout` (POST), panel `/Index` y `/`. Se conserva `/Proveedores`. No cambian rutas ni DTOs del microservicio.
- `DemoPerfilRequeridoFilter` verifica la presencia de un perfil en la sesión antes de ejecutar PageModels protegidos. Login solicita JWT mediante el `DemoTokenProvider` existente antes de activar el perfil. No se añade otro esquema de autenticación; la API sigue validando JWT y roles. Retirar también este filtro y Login/Logout de demo al integrar la identidad definitiva.
- El menú permite cambiar perfil y cerrar sesión con antiforgery. Logout elimina solo el perfil de la sesión actual; no invalida tokens compartidos por otras sesiones demo. Los errores 401/403/409 se conservan en el cliente; un error de guardado mantiene el modal y los datos.
- El panel usa `Total` de tres solicitudes (Todos, Activos, Inactivos) con tamaño 1. No deriva totales de los elementos de la primera página ni muestra ceros inventados si el servicio falla.
- La vista principal filtra Activos; Eliminados usa Inactivos. Eliminar llama al DELETE existente, Reactivar al PATCH existente. El frontend oculta y rechaza acciones de Cajero y la API conserva la autorización definitiva.
- Desde el fortalecimiento del 19 de noviembre, `ProveedoresApiClient.BuscarAsync` delega una consulta paginada a la API. El parámetro existente `busqueda` filtra nombre, teléfono y correo en MongoDB antes de ordenar y paginar; no se descargan todas las páginas en el frontend.
- No cambian puertos de uso habitual, variables, claims, Docker, Keycloak ni datos iniciales. Los recursos de Bootstrap, iconos e Inter se sirven localmente. La identidad continúa marcada DEMO TEMPORAL.

## 22. Validaciones reforzadas (19 de noviembre de 2026)
- Sin cambios en rutas, DTOs de proveedores, puertos, claims o variables. Domain conserva la autoridad sobre nombre, teléfono y correo; ver `VALIDACIONES_Y_SEGURIDAD.md`.
- POST/PUT inválidos conservan HTTP 400 y `code`/`detail`. Se añade el miembro opcional `errors` a ProblemDetails: objeto con claves `nombreEmpresa`, `numeroEmpresa`, `correoReferencia` y listas de mensajes. El frontend reconoce únicamente esas claves y conserva el modal y los valores.
- JSON o parámetros de tipo incorrecto devuelven 400 con `code=Solicitud.Invalida` y un mensaje controlado. Identificadores malformados en la API conservan 404; no se cambió esa semántica.
- `busqueda`: hasta 150 caracteres tras recorte; blanco significa sin filtro; controles/invisibles inválidos devuelven 400. Se amplía la búsqueda existente al correo, sin parámetros nuevos. Nombre compara sin tildes; correo y teléfono se buscan literalmente, ignorando mayúsculas. Los metacaracteres se escapan; no son operadores MongoDB.
- Estado/orden numéricos fuera del enum devuelven 400. Se conservan límites de página/tamaño, orden global previo a paginación y desempate `_id` ascendente.
- No se migra ningún dato ni se añade un validador de colección que impida leer documentos históricos. Los datos históricos incompatibles requieren revisión explícita; no se publican registros personales ni se corrigen automáticamente.
- La prohibición de desactivar proveedores con productos activos continúa pendiente de la integración HTTP; no está activada.

## 23. Ajustes finales de usabilidad
- Política comercial vigente: teléfono nacional de 8 dígitos ASCII, inicio 4/6/7, sin prefijos +591 ni 591. Misma forma string y mismas respuestas Result/400; no se migran datos.
- Búsqueda: el parámetro `busqueda` mantiene forma y campos; ahora ordena grupos por prefijo de nombre, parcial de nombre y solo teléfono/correo antes de aplicar el criterio solicitado y `_id` estable. Sin búsqueda se conserva el orden habitual. Sin cambios de índices, puertos, rutas, DTOs ni variables.
- UI: solo Nombre A-Z/Z-A, con los parámetros existentes `ordenarPor=Nombre` y `descendente`. Otros criterios HTTP siguen disponibles para clientes internos. Búsqueda por fetch al GET Razor existente con debounce 300 ms y botón Limpiar; no se expone una ruta nueva.
- Inventario, evidencia y limitaciones: `AJUSTES_USABILIDAD.md`.

## 24. Keycloak real y validación en el Gateway (19 de noviembre de 2026)
- Realm `puntomarket` importado desde `docker/keycloak/realm-puntomarket.json` (servicio `keycloak` con `start-dev --import-realm`). Roles `Administrador` y `Cajero`; cliente público `puntomarket-web` con Direct Access Grants; mapper de audiencia que agrega `aud=proveedores-api`; política de contraseña (mínimo 8, número, mayúscula, minúscula y carácter especial); access token de 30 minutos. El import solo actúa si el realm no existe en el volumen `keycloak-data`.
- El administrador inicial se crea con `PUNTOMARKET_ADMIN_USERNAME`, `PUNTOMARKET_ADMIN_PASSWORD` y `PUNTOMARKET_ADMIN_EMAIL` del `.env` (sustitución de variables en el import; nada de esto se versiona).
- Gateway: `JwtBearer` con `Authentication:Jwt:Authority` (`http://keycloak:8080/realms/puntomarket`), sin validar audiencia (la validan los servicios). Los roles de `realm_access.roles` se convierten en claims de rol. Políticas en las rutas (`AuthorizationPolicy`): `/api/usuarios/**` exige `Administrador`, salvo `/api/usuarios/yo` y `/api/usuarios/yo/password` (token válido); Clientes, Productos y Proveedores exigen token válido; las rutas `*/salud` son anónimas.
- Proveedores en `docker-compose.yml`: `Authentication__Jwt__Authority`, `Audience=proveedores-api`, `RequireHttpsMetadata=false`, `RoleClaimPaths__0=realm_access.roles` y `RoleMapping` Administrador/Cajero hacia los mismos nombres. Sin cambios en el código de Proveedores.
- Issuer: el `iss` depende del host con el que se pide el token. Un token pedido a `localhost:8180` trae `iss=http://localhost:8180/...` y el Gateway y los servicios lo rechazan (401). Los tokens deben pedirse dentro de la red de Docker (`http://keycloak:8080`), como hará el login del Gateway.

## 25. Login y sesión (19 de noviembre de 2026)
- Gateway: `POST /api/auth/login` (anónimo) recibe `{ "username", "password" }`, pide el token a Keycloak (`Keycloak:TokenEndpoint`, cliente `Keycloak:ClientId`, `grant_type=password`) y llama a `GET /api/usuarios/yo` de Usuarios (`Usuarios:BaseUrl` y `Usuarios:CurrentUserPath`) con ese token. Responde `200 { accessToken, expiresIn, username, nombreCompleto, rol, debeCambiarPassword }`. Errores con `{ "error": "..." }`: `400` datos incompletos, `401` credenciales incorrectas o usuario no habilitado (inactivo o no registrado en Usuarios), `503` Keycloak o Usuarios no responden.
- `POST /api/auth/logout` (token válido) responde `204`; si el cuerpo trae `{ "refreshToken": "..." }` lo revoca en Keycloak. El Razor no guarda refresh token, por lo que el cierre de sesión solo elimina la sesión del servidor.
- Razor: `Frontend/Auth` (adaptador, sesión, `TokenProvider` Singleton con `Lazy<T>`, `BearerTokenHandler`) y páginas `/Auth/Login`, `/Auth/Logout` y `/Auth/CambiarPasswordObligatorio` (provisional). El token y los datos del usuario viven en la sesión del servidor (cookie HttpOnly, 30 minutos de inactividad); una sesión cuyo token venció se considera cerrada. Mientras `debeCambiarPassword` sea verdadero, toda ruta fuera de `/Auth` redirige al cambio obligatorio. Los adaptadores nuevos se registran con `AddGatewayAdapter<TInterface, TImplementation>()` para enviar el Bearer.
- Variables y rutas existentes no cambian. El login requiere que Usuarios exponga `GET /api/usuarios/yo` con los campos `username`, `nombreCompleto`, `rol`, `activo` y `debeCambiarPassword`.

## 26. Usuarios: JWT, Keycloak y correo (19 de noviembre de 2026)
- Api de Usuarios: `JwtBearer` configurado con `Authentication:Jwt:Authority`, `Audience` (`usuarios-api`) y `RequireHttpsMetadata`; en `docker-compose.yml` se fijan por variables de entorno. Los roles de `realm_access.roles` se convierten en claims de rol. El realm importado agrega el mapper `audiencia-usuarios-api`, de modo que los tokens traen `aud=usuarios-api` además de `proveedores-api`; los volúmenes de Keycloak ya creados no se actualizan solos (ver §24).
- `UsuarioActualHttp` lee `preferred_username` (`Username`) y el rol desde los claims del token. `IUsuarioActual` de Application aún expone `Id` y `Rol`; `Username` está en `IUsernameActual` (Api) hasta que Application lo incorpore.
- Infrastructure: `KeycloakGestorDeIdentidad` (adaptador de la API de administración: crear usuario con rol, cambiar contraseña, verificar contraseña actual, activar o desactivar) y `SmtpEnviadorCorreo.EnviarCredenciales`. Los puertos `IGestorDeIdentidad` e `IEnviadorCredenciales` están temporalmente en Infrastructure con las firmas de los contratos, a la espera de pasarlos a Application. Las contraseñas se crean con `temporary=false`: el cambio forzado lo controla `DebeCambiarPassword`.
- Variables de usuarios-ms: `Keycloak__BaseUrl`, `Keycloak__Realm`, `Keycloak__LoginClientId`, `Keycloak__AdminUserName` y `Keycloak__AdminPassword` (estas dos toman `KEYCLOAK_ADMIN_USER` y `KEYCLOAK_ADMIN_PASSWORD` del `.env`) y `Smtp__*` (desde `SMTP_*`).

## 27. Orden de arranque y audiencias (19 de noviembre de 2026)
- El realm agrega los mappers `audiencia-clientes-api` y `audiencia-productos-api`: el token trae `aud` con `clientes-api`, `productos-api`, `proveedores-api` y `usuarios-api`. El import solo se aplica si el realm no existe. En un realm persistido, revisar y actualizar los mappers mediante administración de Keycloak con respaldo; no recrear ni borrar el volumen para aplicar estos cambios.
- `docker-compose.yml`: Keycloak expone `/health/ready` en el puerto de gestión (9000, no publicado, `KC_HEALTH_ENABLED=true`) y su healthcheck lo consulta con `bash` y `/dev/tcp`, porque la imagen no trae `curl`. Los microservicios esperan a su base de datos y a Keycloak (`service_healthy`); el Gateway espera a Keycloak y a los cuatro microservicios sanos; el Razor espera al Gateway sano. Los servicios .NET usan el mismo método (`bash` y `/dev/tcp`, sin instalar paquetes en los Dockerfile): `GET /api/salud` en Usuarios, Clientes y Productos, `GET /health` en Proveedores, `GET /Auth/Login` en el Razor y una conexión TCP al puerto 8080 en el Gateway.

## Verificación ejecutada del merge local con 3eefb83

- Restore de los 25 proyectos correcto. El primer intento restringido no pudo consultar NuGet (NU1900); se repitió con acceso a red y terminó sin advertencias.
- Build completo: 0 errores y 0 advertencias, sin NU1902/NU1903.
- Suite de integración de solución: Gateway 16, Frontend común 33, Usuarios 64 y Proveedores 215; total 328 aprobadas, 0 fallidas y 0 omitidas. Las pruebas de Proveedores usaron MongoDB local y bases temporales aisladas. La suite propia se ejecutó también por separado: 215 aprobadas.
- JavaScript: 60 casos de campos, corrección de errores y doble envío, y pruebas del buscador dinámico aprobados.
- Ambos Compose validados mediante Docker con variables sintéticas (sin credenciales reales ni conexiones). Se comprobó que los puertos predeterminados no se solapan y que los proyectos, bases y volúmenes son diferentes.
- Construidas las seis imágenes .NET comunes y las cuatro imágenes .NET demo, sin arrancar ni recrear contenedores.
- Se verificó conservación de 196 archivos de código/configuración del equipo y 111 de los componentes propios, sin cambios funcionales respecto a las ramas de origen. El Compose común permanece idéntico a main.
- Los contenedores existentes conservan IDs, fechas de arranque y montajes. La lista y metadatos de volúmenes permanecen iguales. Los cinco contenedores demo existentes siguen healthy; no se atribuye su estado al arranque de la nueva integración.
- Sintaxis del script PowerShell y enlaces de las guías revisados; no se ejecutaron scripts de escritura o ciclo de vida.

Límites: no se inició el entorno común por acuerdo del usuario; no se verificaron login real de Keycloak, envío SMTP, datos/esquemas reales de MySQL/PostgreSQL ni el recorrido completo frontend-Gateway-APIs común. Las pruebas de Usuarios/Gateway usan dobles HTTP y no acreditan conectividad real. Clientes y Productos compilan y construyen sus imágenes, pero no tienen suites de prueba propias en esta solución. El caso de creación de Usuarios y su prueba anterior siguen excluidos en los csproj de main; no se reactivaron ni se implementaron funciones ajenas. El frontend común aún no ofrece el CRUD Proveedores.

La configuración de audiencia del Gateway común sigue como en main (ValidateAudience=false); Proveedores mantiene firma, issuer, audience y lifetime habilitados. Revisar esa decisión con el equipo antes de producción, sin cambiarla unilateralmente durante el merge. No se actualizó ningún realm persistido: sus mappers reales deben comprobarse al arrancar el entorno común.

La comprobación final retiró únicamente un espacio al final de una línea de `Frontend/wwwroot/css/presentacion.css` heredada de main, sin alterar reglas CSS.
