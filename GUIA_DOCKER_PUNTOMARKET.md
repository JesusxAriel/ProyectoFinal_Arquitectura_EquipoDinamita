# Docker PuntoMarket — entorno común y demostración

## Entorno común: referencia principal

El archivo raíz `docker-compose.yml` conserva literalmente los once servicios de origin/main (3eefb83): Keycloak, Gateway, frontend, cuatro APIs y cuatro bases independientes. No incorpora DevIdentity ni el selector de perfiles demo.

Requiere Docker Desktop con motor Linux, Compose y credenciales/configuración local en `.env`. Revisar `.env.example`; copiarlo solo si no existe `.env`, sin sobrescribir un archivo local. El .env anterior de la demo no contiene necesariamente las variables comunes. Nunca publicar contraseñas o tokens.

```powershell
docker compose config --quiet
# Ejecutar el arranque únicamente tras revisar bases, credenciales y puertos:
docker compose up --build -d
docker compose ps
```

| Servicio común | Puerto host predeterminado | Variable |
|---|---:|---|
| frontend | 5100 | FRONTEND_PUBLISHED_PORT |
| gateway | 5000 | GATEWAY_PUBLISHED_PORT |
| Keycloak | 8180 | KEYCLOAK_PUBLISHED_PORT |
| Usuarios | 5003 | USUARIOS_PUBLISHED_PORT |
| Clientes | 5001 | CLIENTES_PUBLISHED_PORT |
| Productos | 5004 | PRODUCTOS_PUBLISHED_PORT |
| Proveedores | 5005 | PROVEEDORES_PUBLISHED_PORT |
| MongoDB común | 27018 | PROVEEDORES_DB_PUBLISHED_PORT |
| MySQL Usuarios | 3308 | USUARIOS_DB_PUBLISHED_PORT |
| PostgreSQL Productos | 5433 | PRODUCTOS_DB_PUBLISHED_PORT |
| MySQL Clientes | 3310 | CLIENTES_DB_PUBLISHED_PORT |

El frontend común usa `GatewayApi__BaseUrl`, el Gateway resuelve los servicios por DNS y Proveedores usa `proveedores-db:27017`, con autenticación y `PROVEEDORES_DB_NAME`. Sus credenciales siguen siendo las de main. No se ha creado, renombrado o migrado ninguna base durante el merge.

## Demostración separada de Proveedores

El archivo `docker-compose.demo.yml` conserva cinco servicios: frontend, gateway, proveedores-api, identity-dev y mongodb. Conserva el nombre de proyecto `puntomarket`, los nombres de servicios, redes y volúmenes originales para no cambiar la persistencia existente.

```powershell
docker compose -f docker-compose.demo.yml config --quiet
docker compose -f docker-compose.demo.yml up --build -d
docker compose -f docker-compose.demo.yml ps
docker compose -f docker-compose.demo.yml logs --tail 100 proveedores-api
```

| Servicio demo | Puerto host predeterminado | Variable |
|---|---:|---|
| frontend | 15100 | FRONTEND_PORT |
| gateway | 15000 | GATEWAY_PORT |
| API | 15200 | PROVEEDORES_API_PORT |
| DevIdentity | 15300 | DEV_IDENTITY_PORT |
| MongoDB demo | 27028 | MONGO_PORT |

Un `.env` previo puede conservar puertos distintos, como MongoDB 27018. Las variables de terminal prevalecen sobre `.env`. Comprobar los puertos efectivos antes de iniciar ambas pilas; los cambios de valores predeterminados no recrean los contenedores existentes. No se modifica `.env` automáticamente.

En /Login elegir Administrador o Cajero. DevIdentity emite JWT RS256 de demostración con audiencia proveedores-api y realm proveedores-dev; no constituye identidad de producción. El Gateway demo reenvía Authorization, mientras la API valida JWT y roles. La API usa issuer público y MetadataAddress interno para discovery/JWKS. El mismo código API se configura para Keycloak en el entorno común, sin duplicar autenticación.

## Persistencia: tres destinos diferentes

| Entorno | Destino | Persistencia |
|---|---|---|
| Común | proveedores-db:27017; base PROVEEDORES_DB_NAME | puntomarket-microservicios_proveedores-db-data |
| Demo | mongodb:27017; proveedores_db | puntomarket_mongodb_data |
| Windows local | localhost:27017, según appsettings | Directorio del servicio MongoDB de Windows |

Los nombres indicados asumen los nombres de proyecto predeterminados, sin `-p` ni COMPOSE_PROJECT_NAME externo. La demo conserva también `puntomarket_frontend_keys`. No compartir, eliminar o copiar volúmenes entre entornos. Compass debe usar el puerto publicado del entorno elegido; conectar a otra base no significa que los documentos originales desaparecieron.

El seed de Proveedores valida siete proveedores y no sobrescribe documentos existentes ni repite actualizaciones administrativas históricas. El inicio de una API puede inicializar índices/seed según sus opciones; no se arrancó el común durante este merge. No borrar volúmenes para aplicar nuevas audiencias a Keycloak: revisar el realm persistido y actualizar sus mappers mediante administración con respaldo.

## Health y diagnóstico

El común conserva healthchecks bash/TCP o HTTP y espera bases/Keycloak sanos; la demo utiliza curl y sus healthchecks propios. El Dockerfile de Proveedores conserva noble, curl y usuario sin privilegios, compatible con ambos mecanismos.

Revisar `docker compose logs` para el común y añadir siempre `-f docker-compose.demo.yml` para la demo. No compartir puertos de frontend/Gateway/Mongo entre pilas. Un 401 requiere comprobar issuer, firma/JWKS, audiencia y vigencia; un 403 requiere revisar roles. No desactivar validaciones para resolver problemas de DNS. El health no acredita por sí solo el funcionamiento del login ni de toda la base de datos.

## Pruebas y operación

```powershell
dotnet restore Proyecto_Microservicios.slnx
dotnet build Proyecto_Microservicios.slnx --no-restore
$env:PROVEEDORES_TEST_MONGO='mongodb://localhost:27017'
dotnet test Proyecto_Microservicios.slnx --no-restore
node scripts/probar-validaciones-frontend.cjs
node scripts/probar-busqueda-dinamica.mjs
```

Las pruebas Mongo usan bases temporales únicas que eliminan al finalizar, sin tocar proveedores_db. Los scripts `scripts/verificar-docker.ps1` y `scripts/smoke-test.sh` realizan escrituras de negocio: pertenecen exclusivamente a la demo y no se ejecutaron durante esta integración. El primero fuerza su archivo Compose y exige `-PermitirEscrituras`; `-ProbarCicloDeVida` implica parar/reiniciar la demo. No usar estos scripts en el común o con datos reales.

Para detener aplicaciones en el futuro, `docker compose stop` conserva contenedores y volúmenes; añadir `-f docker-compose.demo.yml` en la demo. No utilizar `down -v` ni limpiar volúmenes. No se ejecutaron operaciones de ciclo de vida durante el merge.

## Ejecución local sin Docker

La demo mantiene los launchSettings originales. Con MongoDB Windows en 27017, ejecutar en terminales distintas:

```powershell
dotnet run --project DevIdentity/DevIdentity.Demo
dotnet run --project Servicio4.Proveedores/Proveedores.API
dotnet run --project ApiGateway/ApiGateway.Demo
dotnet run --project Frontend/Frontend.Demo
```

Los puertos locales habituales son 5300/5200/5000/5100. No ejecutarlos sobre puertos ocupados por Docker. El entorno común depende de Keycloak y sus bases/configuración propios; no basta ejecutar este conjunto demo para probarlo.

Ver [README](README.md), [acuerdos Grupo 2](INTEGRACION_GRUPO2.md) y [contrato Grupo 1](GUIA_INTEGRACION_GRUPO1.md). La UI de Proveedores todavía debe incorporarse al frontend común mediante su sesión y adaptadores. Los resultados del merge figuran en el informe de verificación y no acreditan un arranque completo que no se haya ejecutado.
