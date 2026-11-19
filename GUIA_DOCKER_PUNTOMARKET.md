# Docker de PuntoMarket — Grupo 1 Proveedores

Esta guía corresponde a **micro-proveedor**: cinco servicios con DevIdentity, no el Compose conjunto con Keycloak de otras ramas. Para diferencias entre ramas ver [INTEGRACION_GRUPO2.md](INTEGRACION_GRUPO2.md). Código activo: Servicio4.Proveedores; no usar los obj locales de Servicio4 como contexto de construcción.

## 1. Requisitos y apertura de Docker Desktop

En Windows, instalar/abrir Docker Desktop con virtualización y backend compatible (WSL 2 según la instalación); seleccionar contenedores Linux y esperar al motor. Comprobar requisitos oficiales para la edición/build de Windows utilizada. No desactivar seguridad para sortear incompatibilidades.

```powershell
docker version
docker compose version
# Si esta CLI está disponible y Desktop todavía no está iniciado:
docker desktop start
```

Docker version debe mostrar Client y Server. Para ejecutar contenedores no hace falta instalar el SDK ni MongoDB en Windows. Para pruebas locales sí se requieren .NET 10 SDK y Node.js.

## 2. Preparar .env sin sobrescribirlo

Desde la raíz de una copia de micro-proveedor:

```powershell
if (-not (Test-Path -LiteralPath .env)) { Copy-Item .env.example .env }
```

.env.example solo contiene puertos sin secretos. Copiarlo es opcional si se aceptan los valores predeterminados. No copiar plantillas .env de otras ramas: sus nombres y servicios son distintos. Nunca publicar .env ni credenciales.

| Variable | Predeterminado | Servicio / puerto interno |
|---|---|---|
| FRONTEND_PORT | 5100 | frontend / 8080 |
| GATEWAY_PORT | 5000 | gateway / 8080 |
| PROVEEDORES_API_PORT | 5200 | proveedores-api / 8080 |
| DEV_IDENTITY_PORT | 5300 | identity-dev / 8080 |
| MONGO_PORT | 27018 | mongodb / 27017 |

Todos se publican en 127.0.0.1. Cambiar los puertos ocupados en .env; no terminar procesos de otro integrante. Las variables de terminal tienen precedencia sobre .env. DEV_IDENTITY_PORT actualiza emisor y Authority juntos. No se muestran aquí los valores del .env personal.

## 3. Construir y levantar los cinco servicios

```powershell
docker compose config --quiet
docker compose up --build -d
docker compose ps
```

El Compose único es docker-compose.yml, proyecto puntomarket, sin container_name. La primera descarga/compilación tarda más. frontend usa http://gateway:8080; Gateway apunta a proveedores-api:8080 e identity-dev:8080; API usa mongodb:27017. localhost dentro de un contenedor no representa otro servicio.

Los cuatro Dockerfiles usan SDK/ASP.NET Core 10.0-noble, publicación Release, caché de restore y usuario app/APP_UID. MongoDB oficial se fija a 8.0.38-noble. Los .dockerignore excluyen bin/obj, claves, configuraciones privadas y datos. No se copian bases de Windows.

## 4. Comprobar estado, acceso y logs

```powershell
docker compose ps
docker compose logs --tail 100
docker compose logs -f proveedores-api
docker compose logs -f gateway
docker compose exec -T mongodb mongosh --quiet --eval 'db.adminCommand({ping:1})'
```

Esperar cinco servicios running/healthy: frontend, gateway, proveedores-api, identity-dev y mongodb. Mongo/identidad saludables preceden a la API; Gateway y frontend dependen de sus servicios. Los health HTTP prueban el proceso, no disponibilidad permanente de toda la cadena.

Con puertos predeterminados: abrir http://localhost:5100/Login; Gateway http://localhost:5000/api/proveedores; Swagger de la API http://localhost:5200/swagger. Usar los puertos efectivos de .env si se cambiaron. Consultar /health en cada proceso HTTP; no confundir el health del Gateway con el de Proveedores.

El login es un selector demo sin credenciales personales. Administrador escribe y consulta; Cajero consulta. La API aplica permisos definitivos. Keycloak y Usuarios reales no se inician aquí.

## 5. MongoDB, Compass y seed

Compass del host: **mongodb://localhost:27018** (o MONGO_PORT), base proveedores_db y colección proveedores. No usar 27017 si corresponde a MongoDB de Windows. La demo Docker no exige contraseña; el binding es loopback y no debe exponerse en LAN ni utilizarse como despliegue productivo.

Mongo usa volumen nombrado mongodb_data, normalmente puntomarket_mongodb_data. Red datos-proveedores interna solo para API/Mongo; compass-local conecta únicamente Mongo para habilitar su puerto loopback; aplicaciones conecta los procesos HTTP. Otros microservicios conservarán bases, credenciales y redes propias.

En volumen vacío, el seed inserta PIL Andina, EMBOL, Sofía, CBN, Unilever, Industrias Venado y Arcor desde código. No importar SQL ni registros manuales personales. origenSeed y $setOnInsert impiden duplicar o sobrescribir esos registros al reiniciar. Un volumen usado conserva altas/bajas posteriores: no se reinicia a siete documentos.

frontend_keys conserva Data Protection/antiforgery al recrear el frontend; las sesiones siguen en memoria y puede requerirse elegir el perfil otra vez. Las claves viven en volumen, no en Git ni en la imagen.

## 6. JWT público e interno

Issuer público predeterminado: http://localhost:5300/realms/proveedores-dev. Audience: proveedores-api. API conserva Authority público y MetadataAddress interno http://identity-dev:8080/realms/proveedores-dev/.well-known/openid-configuration. JWKS usa el origen de consulta; el issuer firmado sigue configurado, no se decide mediante Host.

Se mantienen validaciones de firma RS256/JWKS, emisor, audiencia, vigencia y roles. Gateway propaga Bearer sin validarlo aquí. DevIdentity solo funciona en Development, genera RSA al iniciar y no implementa identidades personales ni refresh OAuth. Tras reiniciarlo puede ser necesario renovar tokens/refrescar JWKS; no desactivar validaciones para resolver 401.

Consulta sin escrituras, ajustando puertos si corresponde:

```powershell
$identidad = Invoke-RestMethod -Method Post -Uri http://localhost:5000/dev-auth/token `
  -ContentType 'application/json' -Body '{"perfil":"Administrador"}'
Invoke-RestMethod -Uri http://localhost:5000/api/proveedores `
  -Headers @{Authorization="Bearer $($identidad.access_token)"}
```

No imprimir ni compartir el token. Para consumir desde otros grupos ver [contrato Grupo 1](GUIA_INTEGRACION_GRUPO1.md).

## 7. Reinicios, persistencia y mantenimiento

```powershell
# Detener y recuperar sin borrar contenedores.
docker compose stop
docker compose start
# Reiniciar sin reconstruir.
docker compose restart
# Aplicar cambios de código.
docker compose up --build -d
# Retirar contenedores/redes manteniendo datos.
docker compose down
docker compose up -d
# Consultar volúmenes, sin eliminarlos.
docker volume ls
docker volume inspect puntomarket_mongodb_data
```

**down conserva los volúmenes; down -v los elimina y borra datos.** No usar -v, volume prune ni eliminación de carpetas para reiniciar o comprobar persistencia. Mantener el nombre de proyecto: otro -p utiliza otros volúmenes y puede aparentar pérdida de datos.

Para comprobar persistencia manualmente: registrar IDs/conteo de datos de prueba, reiniciar API y verificar seed sin duplicados; reiniciar Mongo y consultar de nuevo; realizar down/up sin -v y comprobar IDs, estado y datos. No probar escrituras en datos personales ni borrar la colección para recuperar siete registros.

El script existente siguiente es **de escritura y ciclo de vida**, no una simple inspección. Usarlo solo con autorización y pila Docker de pruebas:

```powershell
./scripts/verificar-docker.ps1 -PermitirEscrituras -ExigirSoloSeed -ProbarCicloDeVida
```

ExigirSoloSeed falla si ya hay más registros: omitir el flag al repetir, sin borrar nada. El script añade una evidencia y la conserva inactiva; cada ejecución puede añadir otra. Admite FrontendPort/GatewayPort/ApiPort/IdentityPort. No se ejecuta como parte de esta limpieza documental.

## 8. Pruebas automatizadas

Con Mongo Docker disponible en el puerto de pruebas correspondiente:

```powershell
$env:PROVEEDORES_TEST_MONGO = 'mongodb://localhost:27018'
dotnet restore Proyecto_Microservicios.slnx
dotnet build Proyecto_Microservicios.slnx --no-restore
dotnet test Servicio4.Proveedores/Proveedores.slnx --no-restore
node scripts/probar-validaciones-frontend.cjs
node scripts/probar-busqueda-dinamica.mjs
```

Las pruebas Mongo crean bases temporales únicas, no usan proveedores_db. Sin la variable se omiten. DockerIdentityTests cubre discovery público/interno y RS256; MongoSeedApiTests cubre API y seed/reinicio de una base temporal.

En la fase Docker anterior se registraron build sin errores/advertencias y 215 pruebas aprobadas, con CRUD/roles/persistencia y cinco servicios saludables; son evidencias históricas, no pruebas nuevas de esta limpieza ni de otras ramas. La UI no se validó exhaustivamente en todos los navegadores. No se afirma un estado actual de contenedores solo por conservar esos resultados.

## 9. Ejecución local sin Docker

Con .NET 10 SDK y MongoDB Windows en 27017, en terminales distintas:

```powershell
dotnet run --project DevIdentity/DevIdentity.Demo
dotnet run --project Servicio4.Proveedores/Proveedores.API
dotnet run --project ApiGateway/ApiGateway.Demo
dotnet run --project Frontend/Frontend.Demo
```

Los appsettings/launchSettings locales mantienen localhost y puertos habituales. MetadataAddress es opcional. No dejar variables de otro entorno en la terminal ni compartir puertos con Docker. No se modifica MongoDB de Windows para preparar Compose.

## 10. Diagnóstico y futura integración

| Síntoma | Revisar |
|---|---|
| Solo Docker Client | Iniciar Desktop y motor Linux; comprobar requisitos de instalación |
| Puerto ocupado | Cambiar la variable correspondiente sin sobrescribir .env propio |
| 401 | Issuer/Authority, audience, MetadataAddress/JWKS y hora; no desactivar validaciones |
| 403 | Rol válido sin permiso; Cajero no escribe |
| 502/503 | Salud, logs, DNS y destinos internos de YARP |
| Compass no conecta | Puerto host 27018 o el configurado, no puerto interno 27017 |
| No hay exactamente siete | Volumen conserva altas/bajas previas; no eliminarlo para forzar seed |
| Cambió fuente pero no app | restart no compila: usar up --build -d |
| Sesión perdida | Volver a elegir perfil y revisar frontend_keys/logs |
| Error al descargar imágenes | Red/proxy/certificados del equipo; no incluir credenciales en Dockerfiles |

Para incorporar Grupo 2, acordar primero qué estructura de Gateway/Frontend/Servicio4 se adoptará. El Compose conjunto de otras referencias utiliza otros servicios, variables y Keycloak; no intercambiar .env ni arrancar dos proyectos que compitan por puertos. Añadir cada servicio real con su base/volumen/red y cluster YARP propios, health y contratos confirmados; sin servicios ficticios ni rutas inventadas.

Fuente: [INTEGRACION_GRUPO2.md](INTEGRACION_GRUPO2.md). Esta guía no integra Keycloak, Usuarios ni realiza merge; preserva el despliegue actual del Grupo 1.
