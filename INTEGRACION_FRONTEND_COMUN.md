# Clientes y Proveedores en el frontend común

## Implementación

Las rutas `/Clientes` y `/Proveedores` pertenecen al proyecto `Frontend/PuntoMarket.Frontend.csproj`. No referencian Frontend.Demo ni bibliotecas de persistencia o de los microservicios. Los adaptadores de `Frontend/Catalogos` usan `AddGatewayAdapter<I,T>()`: el único JWT es el de la sesión común, enviado por `BearerTokenHandler`. No hay DevIdentity, selector de perfil, refresh ficticio, localStorage ni segunda sesión en estos módulos.

`ResultadoOperacion<T>` conserva estado HTTP, mensaje, código y errores por campo de ProblemDetails sin modificar el Result de autenticación. Los errores 401/403 se muestran controladamente en estas páginas; 401 ofrece volver a `/Auth/Login`. No se cambian tokens ni middleware compartido. Las redirecciones de búsqueda hacia `/Auth/` respetan login o cambio obligatorio. Los errores 5xx no exponen detalles internos.

`PaginaCatalogo` comprueba usuario autenticado y roles antes de ejecutar handlers. Las escrituras verifican además sus permisos: Clientes permite crear/editar a Administrador y Cajero; solo Administrador cambia estado. Proveedores permite escribir solo a Administrador. Las APIs siguen siendo la autoridad de autorización y validaciones de negocio.

Editar y Detalles se cargan por POST con antiforgery. Los IDs y el identificador de edición van en campos ocultos, nunca en links ni form actions. Los adaptadores mantienen las rutas REST originales con IDs. Cerrar un modal vuelve al GET del listado conservando filtros; no se crea estado de edición en sesión. Filtros y páginas pueden aparecer en la URL. Los IDs ocultos no son secretos: siempre se validan en servidor.

La búsqueda comparte un único motor JavaScript entre ambos módulos comunes: debounce 300 ms, AbortController, revisión de respuestas obsoletas y paginación del servidor. El texto de búsqueda se envía como dato. Los estilos están acotados a `.catalogo`. Clientes mantiene normalización Unicode a mayúsculas antes de validar/enviar y al salir del campo. Proveedores conserva sus validaciones tempranas y las definitivas de la API, sin alterar nombres comerciales ni contratos. Los detalles de Proveedores usan únicamente el DTO público existente. Esos DTOs no incluyen la versión de concurrencia interna: la migración no modifica ese contrato.

## Configuración conservada

El Compose común utiliza el realm `puntomarket`, Authority `http://keycloak:8080/realms/puntomarket`, audiences `clientes-api` y `proveedores-api` y roles de `realm_access.roles`. Gateway reenvía las rutas de ambos servicios con políticas de token válido; cada API valida también audiencia y permisos. El claim `sub` se conserva como texto para auditoría (compatible con GUID). El frontend llama a Gateway, nunca a MySQL/MongoDB.

No se modifican Docker Compose, configuración JWT, Keycloak, Usuarios, Gateway común, bases ni volúmenes. `Clientes:SeedGenericoEnabled=false` continúa siendo exclusivo del Demo. Cambiar el seed del entorno común es una decisión pendiente; en una tabla vacía sigue siendo posible crear el genérico con los valores por defecto. No se ocultan filas después de paginar ni se borra el registro histórico.

## Pendientes del Grupo 2

- El layout conserva enlaces históricos a `/Account` y `/Users`, excluidos de compilación por el proyecto común. Solo se añadieron enlaces a Clientes/Proveedores. Grupo 2 debe acordar el destino de perfil, logout, contraseña y Usuarios; no se reactivaron páginas antiguas ni se sustituyó su trabajo.
- `/Auth/CambiarPasswordObligatorio` aún es una pantalla pendiente. Usuarios con cambio obligatorio no pueden completar la operación desde ella.
- La sesión actual usa memoria y el rol del perfil de Usuarios; comprobar que coincide con los roles del JWT. El NameIdentifier del frontend es username y no debe enviarse como auditoría: la API obtiene el sub del Bearer.
- Acordar renovación real, cierre/invalidación global ante 401 y revocación/logout. Los nuevos módulos no implementan una alternativa ni cambian la autenticación compartida.
- El recorrido real Login -> Keycloak -> Usuarios/perfil -> sesión -> Gateway -> consulta de ambos módulos se comprobó con Administrador. Sigue pendiente el recorrido real con Cajero y su respuesta 403; las pruebas automatizadas sí verifican esos permisos.

## Pruebas y límites

Los adaptadores se prueban con HttpMessageHandler simulado, incluyendo el pipeline real de BearerTokenHandler. Las páginas se prueban con WebApplicationFactory y la sesión real, antiforgery y Gateway simulado. No se usan bases persistentes ni contenedores. Las pruebas nuevas se encuentran en Frontend.Tests/Catalogos y Tests/Demo.Tests/CatalogosComunesTests.cs (este último reutiliza la infraestructura TestServer ya disponible; no significa dependencia del frontend común respecto al Demo).

```powershell
dotnet restore Proyecto_Microservicios.slnx
dotnet build Proyecto_Microservicios.slnx --no-restore
$env:PROVEEDORES_TEST_MONGO=''
dotnet test Proyecto_Microservicios.slnx --no-build --no-restore
node scripts/probar-catalogos-comunes.mjs
node scripts/probar-clientes-demo.mjs
node scripts/probar-interfaz-clientes.mjs
node scripts/probar-validaciones-frontend.cjs
node scripts/probar-busqueda-dinamica.mjs
git diff --check
```

La suite Mongo real queda omitida si no se configura su destino temporal. Las pruebas JavaScript usan DOM mínimo y reloj controlado, no sustituyen una revisión visual en navegador. El Demo permanece independiente y sus archivos no se trasladan ni modifican.

## Comprobación real y siguientes pruebas

El 20 de noviembre de 2026 se comprobó el login real con Keycloak, perfil activo de Usuarios y sesión Razor de Administrador. Las consultas de Clientes y Proveedores por Gateway y sus páginas devolvieron HTTP 200; sin JWT devolvieron 401. Se verificaron issuer, audiences, rol y sub GUID sin publicar tokens ni secretos. El usuario confirmó además consulta y creación manual de registros desde el frontend. No se atribuyen esas escrituras a las pruebas automatizadas ni se afirma que edición, baja y reactivación estén verificadas con Keycloak real.

Los 11 servicios comunes quedaron saludables y el Demo detenido, conservando sus contenedores y datos. Ese es el estado del entorno local en esa fecha, no un requisito para otros equipos. Las pruebas posteriores que creen datos o levanten/recreen servicios requieren autorización. Nunca eliminar volúmenes ni recrear keycloak-data. El realm persistido ya fue consultado y conservado; el archivo de importación por sí solo no demuestra su estado efectivo.

Después de autorización y de revisar variables locales sin mostrarlas:

```powershell
# Puerto local alternativo, si debe conservarse el destino ya probado.
$env:PROVEEDORES_DB_PUBLISHED_PORT='27029'
docker compose -f docker-compose.yml config --quiet
# Ejecutar arranque/reconstrucción solo tras autorización:
docker compose -f docker-compose.yml up -d --build
docker compose -f docker-compose.yml ps
docker compose -f docker-compose.yml logs --tail 100 frontend gateway clientes-ms proveedores-ms
```

Abrir `http://localhost:5100/Auth/Login` con los puertos por defecto (respetar los sobrescritos en .env). Tras login: `/Clientes` y `/Proveedores`. Comprobar primero perfil de Usuarios y ambas audiences/issuer/roles del token sin publicarlo. Verificar GET sin JWT=401, Cajero sin permisos=403 y auditoría del sub. Solo con autorización adicional usar clientes/proveedores sintéticos para crear, editar, desactivar y reactivar; nunca modificar registros reales.

La integración real de autenticación y lectura con Administrador está comprobada. La cuenta Cajero y el 403 real siguen pendientes por falta de credenciales autorizadas; no se crearon usuarios ni se cambiaron roles para fabricar esa prueba. La revisión del navegador debe comprobar también cookies: se observaron cookies de sesión antiguas o cruzadas al coexistir ambos frontends sobre localhost. Con cookies nuevas el login funcionó sin bucles; no se modificó la autenticación compartida para cambiar nombres de cookies. Las claves Data Protection y sesiones en memoria requieren seguimiento del Grupo 2 para despliegues estables. El resultado automatizado no acredita por sí solo el CRUD completo contra servicios reales. La regla de no desactivar proveedores con productos activos sigue pendiente de integración HTTP con Productos; esta migración no la implementa.

## Inventario de esta implementación

Archivos nuevos:

- `Frontend.Tests/Catalogos/AdaptadoresTests.cs`
- `Frontend/Catalogos/ClienteModels.cs`
- `Frontend/Catalogos/ClientesApiClient.cs`
- `Frontend/Catalogos/IClientesApiClient.cs`
- `Frontend/Catalogos/IProveedoresApiClient.cs`
- `Frontend/Catalogos/PaginaCatalogo.cs`
- `Frontend/Catalogos/ProveedorModels.cs`
- `Frontend/Catalogos/ProveedoresApiClient.cs`
- `Frontend/Catalogos/RespuestaGateway.cs`
- `Frontend/Catalogos/ResultadoOperacion.cs`
- `Frontend/Pages/Clientes/Index.cshtml`
- `Frontend/Pages/Clientes/Index.cshtml.cs`
- `Frontend/Pages/Proveedores/Index.cshtml`
- `Frontend/Pages/Proveedores/Index.cshtml.cs`
- `Frontend/wwwroot/css/catalogos.css`
- `Frontend/wwwroot/js/catalogos-modales.mjs`
- `Frontend/wwwroot/js/clientes-busqueda.mjs`
- `Frontend/wwwroot/js/clientes-formulario.mjs`
- `Frontend/wwwroot/js/proveedores-busqueda.mjs`
- `Frontend/wwwroot/js/proveedores-formulario.js`
- `INTEGRACION_FRONTEND_COMUN.md`
- `Tests/Demo.Tests/CatalogosComunesTests.cs`
- `scripts/probar-catalogos-comunes.mjs`

Archivos existentes modificados:

- `Frontend/FrontendBootstrap.cs`
- `Frontend/Pages/Shared/_Layout.cshtml`
- `INTEGRACION_GRUPO2.md`
- `README.md`
- `Tests/Demo.Tests/Demo.Tests.csproj`

## Resultado de verificación de esta entrega

Restore y compilación de la solución completa: correctos, 0 errores y 0 advertencias. Suite completa final: 508 aprobadas, 0 fallidas y 14 omitidas de MongoDB real (sin destino de persistencia configurado). Desglose: Frontend 43, Clientes 54, Usuarios 111, Gateway 16, Proveedores 201 aprobadas/14 omitidas, Demo.Tests 83 (56 de regresión previa y 27 del frontend común). Se agregaron 10 casos de adaptadores en Frontend.Tests y 27 de páginas comunes en TestServer.

El nuevo script JavaScript del frontend común y los cuatro scripts previos del Demo pasaron. Ambos Compose validaron con config --quiet y git diff --check terminó sin errores. Se compararon hashes respecto al inicio: Demo, microservicios, Gateway, autenticación compartida, Keycloak y Compose permanecen intactos. Las variables obligatorias del Compose común están presentes en .env; esto no acredita que sus valores coincidan con bases o servicios inicializados. La fase automatizada no operó contra bases persistentes. La comprobación real posterior se describe arriba, incluyendo la creación manual comunicada por el usuario. Los respaldos, logs y reportes operativos permanecen locales y excluidos de Git; no forman parte de esta entrega.
