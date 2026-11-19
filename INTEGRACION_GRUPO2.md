# Integración con Grupo 2 — Estado por rama

**Rama revisada: micro-proveedor, HEAD 77c46f2.** Este documento no incorpora código de otras ramas. Se inspeccionaron archivos actuales y objetos Git locales mediante consultas de solo lectura: sin fetch, pull, merge o checkout. Las referencias origin/* pueden estar desactualizadas respecto al servidor; los hashes delimitan lo observado.

## 1. Implementado en micro-proveedor

| Componente | Implementación real aquí | Fuente |
|---|---|---|
| Proveedores Grupo 1 | CRUD, MongoDB, validaciones, baja/reactivación, auditoría y version | Servicio4.Proveedores/ |
| Gateway | YARP sin validación JWT propia; reenvía Authorization | ApiGateway/ApiGateway.Demo/Program.cs |
| Rutas Gateway | /api/proveedores, /api/proveedores/{**catch-all}, /dev-auth/{**catch-all} | ApiGateway/ApiGateway.Demo/appsettings.json |
| Frontend | Selector Administrador/Cajero, panel y módulo Proveedores | Frontend/Frontend.Demo/ |
| Sesión | Perfil en sesión servidor (60 minutos de inactividad); JWT en caché por perfil | DemoPerfilSession.cs y DemoTokenProvider.cs |
| Login | Pide token demo por /dev-auth/token y activa el perfil al obtenerlo | Pages/Login.cshtml.cs |
| Logout | POST con antiforgery; elimina perfil, no revoca tokens compartidos | Pages/Logout.cshtml.cs |
| Autorización | API valida JWT; lectura Admin/Cajero, escritura Admin; UI restringe acciones | Proveedores.API/Security y Controller |
| Docker | frontend, gateway, proveedores-api, identity-dev y mongodb | Compose actual, proyecto puntomarket |
| Keycloak/Usuarios reales | No presentes ni iniciados por este Compose | No hay servicios ni rutas actuales hacia ellos |

DevIdentity genera RSA en memoria y dos identidades fijas, aud=proveedores-api. No hay contraseñas personales, registro real, flujo OAuth de refresh token ni módulo Usuarios activo. Cambiar perfil no equivale a autenticación individual real.

## 2. Presente en otras referencias locales

Estas implementaciones **no están integradas en micro-proveedor**. Se verificó su código, no su ejecución durante esta limpieza.

| Referencia y hash observado | Código constatado | Diferencia respecto a esta rama |
|---|---|---|
| origin/main — a0cc418 | Estructura conjunta Gateway/, Frontend/, Servicio1.Clientes, Servicio2.Usuarios, Servicio3.Productos y Servicio4.Proveedores; Compose con Keycloak/bases separadas y login común | No sustituye solución/Compose actual sin integración deliberada |
| origin/feature/keycloak-gateway — ce1e556 | Gateway/Security valida JWT y mapea realm_access.roles; políticas YARP; realm en docker/keycloak/realm-puntomarket.json | Aquí el Gateway demo no valida JWT y se usa DevIdentity |
| origin/feature/login-sesion — 7aadd92 | Gateway/Auth para login/logout; Frontend/Auth para sesión/token/middleware; páginas /Auth | Aquí existen /Login, /Logout y caché demo por perfil |
| origin/feature/usuarios-keycloak-api — ca04cc2 | Servicio2.Usuarios con JWT/roles, adaptador administrativo Keycloak, SMTP de activación/credenciales y pruebas | Aquí no hay API Usuarios ni administración de identidades/correo |
| origin/feature/compose-realm — 7a54408 | Compose conjunto con health/ready de Keycloak y dependencias service_healthy de .NET | No equivale a los cinco servicios ni variables de micro-proveedor |

Fuentes consultadas: Gateway/Program.cs, Gateway/Auth/AuthEndpoints.cs, Gateway/appsettings.json, Frontend/FrontendBootstrap.cs, Frontend/Auth/SesionUsuario.cs, Servicio2.Usuarios/src/PuntoMarket.Usuarios.Api/Controllers/UsuariosController.cs, Security/AutenticacionKeycloakExtensions.cs y adaptadores Infrastructure/Identity/KeycloakGestorDeIdentidad.cs e Infrastructure/Email/SmtpEnviadorCorreo.cs.

Además, la solución raíz observada en origin/main referencia Servicio4 y los proyectos Demo antiguos, mientras su Compose construye Gateway/ y Frontend/ comunes. Un build de esa solución por sí solo no acredita la compilación de todos los servicios conjuntos; esa alineación deberá revisarse en la integración futura.

La variante Servicio4 de origin/main conserva namespaces Proveedores.* pero difiere en Docker, descubrimiento JWT y pruebas. Ver [diagnóstico de carpetas](GUIA_INTEGRACION_GRUPO1.md#1-diagnóstico-de-servicio1-y-servicio4). La reorganización autorizada adopta Servicio4 en esta rama conservando su código propio; no sustituye archivos por los de otras referencias.

## 3. Autenticación observada fuera de esta rama

En la referencia de login, Gateway/Auth registra /api/auth/login y /api/auth/logout. El login solicita token a Keycloak y consulta el usuario actual en Usuarios. La sesión común guarda usuario/token por sesión, verifica expiración y dirige a /Auth/CambiarPasswordObligatorio cuando corresponde. Son contratos de esa referencia, no rutas del Gateway demo actual.

El controlador api/usuarios observado exige Administrador para listar, consultar roles y dar de baja. Los demás controladores/contratos deben revisarse antes de consumirlos. No deducir de la política Cajero de Proveedores que Cajero puede listar usuarios. El adaptador Keycloak administra identidades y roles; SMTP gestiona comunicaciones. No copiar paths, puertos o DTOs sin confirmar la implementación elegida.

El Compose conjunto importa un realm y usa variables externas para credenciales. No se reproducen valores secretos ni se leen credenciales del .env local en esta documentación. El acceso a Keycloak del login común se realiza desde Gateway; el token demo actual no es automáticamente válido para esa infraestructura.

## 4. Acuerdos pendientes

1. Servicio4.Proveedores es la ubicación definitiva aquí. Queda acordar Gateway demo/común y Frontend demo/común; el número de carpeta no identifica por sí solo al grupo responsable.
2. Confirmar commits y contrato vigente de Usuarios: rutas, métodos, JSON, errores, paginación, IDs, permisos y relación entre registro, username y sub.
3. Acordar issuer canónico, Authority, transporte discovery/JWKS, audiencias y roles. Proveedores exige proveedores-api y conserva validación de firma, emisor, audiencia y vigencia.
4. Conciliar Docker/configuración: aquí ApiGateway:BaseUrl y cinco servicios; en referencias comunes GatewayApi:BaseUrl y otros paths, nombres/variables. No mezclar plantillas .env ni cambiar solo una URL JWT.
5. Migrar el frontend a identidad por usuario coordinadamente. Sustituir DemoTokenProvider, DemoPerfilSession y filtro demo solo al disponer del nuevo login y contrato.
6. Revisar errores, timeout, cancelación, expiración/logout y cambio obligatorio de contraseña en la implementación elegida.
7. Bases independientes. Auditoría de Proveedores usa sub string, sin FK a la base de Usuarios.
8. Antes de integrar: Proveedores debe funcionar solo; probar regresión, seed idempotente, permisos 401/403 y persistencia.

Riesgo observado: el Compose conjunto configura Authority con keycloak interno; un token obtenido desde otro host/issuer puede ser rechazado. Confirmar iss y metadatos reales, sin desactivar validaciones. Un realm persistido no necesariamente recibe cambios de una nueva plantilla de importación: revisar su operación sin borrar volúmenes.

## 5. Siguiente fase

Contrato actual del Grupo 1: [GUIA_INTEGRACION_GRUPO1.md](GUIA_INTEGRACION_GRUPO1.md). Arranque de esta rama: [GUIA_DOCKER_PUNTOMARKET.md](GUIA_DOCKER_PUNTOMARKET.md), no instrucciones de las referencias conjuntas.

Secuencia para una futura tarea autorizada: confirmar referencias con el equipo -> acordar contratos/estructura -> reconciliar Proveedores y seguridad -> integrar Gateway/identidad -> integrar sesión y cliente UI -> probar servicios con bases independientes y fallos aislados. Esta limpieza no realiza esas modificaciones.

Pendientes propios de Proveedores: integrar HTTP de Productos antes de impedir bajas con productos activos y, si se acuerda cambiar el contrato, versión de cliente/ETag para formularios obsoletos.
