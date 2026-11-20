# PuntoMarket — integración de microservicios

La rama `micro-proveedor` integra localmente `origin/main` (3eefb83) conservando Proveedores en **Servicio4.Proveedores**. El entorno común es la referencia del grupo; la demostración sigue separada y no lo sustituye.

## Estructura

```text
Gateway/ y Gateway.Tests/           YARP común, Keycloak y login
Frontend/ y Frontend.Tests/         Razor común con sesión
Servicio1.Clientes/                 Servicio y base propios
Servicio2.Usuarios/                 API, MySQL, Keycloak, SMTP y pruebas
Servicio3.Productos/                Servicio y base propios
Servicio4.Proveedores/              Domain, Application, Infrastructure, API, Tests
ApiGateway/ApiGateway.Demo/         Gateway temporal
Frontend/Frontend.Demo/             Interfaz temporal completa de Proveedores
DevIdentity/DevIdentity.Demo/        Emisor temporal, solo demo
Proyecto_Microservicios.slnx         Los 25 proyectos comunes y demo
```

## Entorno común (principal)

`docker-compose.yml` conserva los servicios, nombres y volúmenes de main. Revisar `.env.example` y completar un `.env` privado con las variables comunes; **no sobrescribir un .env existente**. El .env anterior de la demo no contiene necesariamente estas variables.

```powershell
docker compose config --quiet
# Arranque futuro, tras revisar credenciales, puertos y bases:
docker compose up --build -d
```

Puertos predeterminados: frontend 5100, Gateway 5000, Keycloak 8180, Usuarios 5003, Clientes 5001, Productos 5004 y Proveedores 5005. MongoDB común se publica en 27018. Keycloak proporciona la audiencia `proveedores-api`; cada API conserva su validación.

El frontend común tiene login/sesión, pero todavía no incorpora las pantallas de Proveedores del demo. No confundir la integración de fuentes con la implementación de ese módulo en la interfaz común.

## Demostración independiente

```powershell
docker compose -f docker-compose.demo.yml config --quiet
docker compose -f docker-compose.demo.yml up --build -d
```

Proyecto `puntomarket`: frontend 15100, Gateway 15000, API 15200, identidad 15300 y MongoDB 27028 por defecto. Las variables `*_PORT` de un .env existente prevalecen: comprobarlas antes de ejecutar ambos entornos. No confundirlas con las variables `*_PUBLISHED_PORT` del entorno común. El selector Administrador/Cajero y DevIdentity son temporales.

La demo conserva sus volúmenes `puntomarket_mongodb_data` y `puntomarket_frontend_keys`. El común utiliza `puntomarket-microservicios_proveedores-db-data`: **son bases distintas**, sin migración ni copia automática. Cambiar un puerto publicado no cambia el volumen ni la base. No borrar volúmenes, especialmente para actualizar un realm persistido.

## Verificación

```powershell
dotnet restore Proyecto_Microservicios.slnx
dotnet build Proyecto_Microservicios.slnx --no-restore
$env:PROVEEDORES_TEST_MONGO='mongodb://localhost:27017'
dotnet test Proyecto_Microservicios.slnx --no-restore
dotnet test Servicio4.Proveedores/Proveedores.slnx --no-restore
node scripts/probar-validaciones-frontend.cjs
node scripts/probar-busqueda-dinamica.mjs
```

Las pruebas Mongo crean bases temporales aisladas y no utilizan la colección real de proveedores. Sin la variable se omiten esas pruebas. Las pruebas de otros servicios no sustituyen una prueba funcional del conjunto con Keycloak, SMTP y bases reales.

## Documentación

- [Integración común](GUIA_INTEGRACION_MICROSERVICIOS.md).
- [Contrato, reglas y arquitectura del Grupo 1](GUIA_INTEGRACION_GRUPO1.md).
- [Acuerdos y estado Grupo 2](INTEGRACION_GRUPO2.md).
- [Docker y separación de entornos](GUIA_DOCKER_PUNTOMARKET.md).
- [Gateway](Gateway/README.md), [frontend común](Frontend/README.md), [Usuarios](Servicio2.Usuarios/README.md) y [API Proveedores](Servicio4.Proveedores/README.md).

Pendientes: incorporar la UI de Proveedores al frontend común coordinadamente con Grupo 2 y conectar Productos por HTTP antes de impedir bajas con productos activos. No publicar secretos, tokens, BSON ni artefactos locales.

## Demo ampliado: Clientes y Proveedores

El entorno temporal ofrece ambos módulos en Frontend/Frontend.Demo: seleccionar perfil en /Login y navegar con el menú Clientes/Proveedores. Conserva las pantallas originales de Proveedores y no depende del login común de Usuarios/Keycloak.

El Gateway demo enruta /api/clientes y /api/proveedores. DevIdentity firma JWT para clientes-api y proveedores-api; cada API conserva su autorización. El demo añade MySQL 8.4 con base clientes_demo_db y volumen nuevo clientes_demo_data, sin reutilizar el volumen común ni modificar MongoDB existente. Utiliza CLIENTES_DB_USER/PASSWORD/ROOT_PASSWORD locales; no publica credenciales.

Puertos nuevos: Clientes 15400 y MySQL demo 13310 (CLIENTES_DEMO_PORT/CLIENTES_DEMO_DB_PORT). Entrada futura: http://localhost:15100/Login, según variables locales. Usar exclusivamente docker-compose.demo.yml. Docker y WSL no se iniciaron durante este desarrollo.

Matriz de roles, validaciones, comandos, pruebas sin bases y puntos de sustitución por Grupo 2: [sección Demo ampliado](INTEGRACION_GRUPO2.md#demo-ampliado-clientes-y-proveedores). Nueva suite Tests/Demo.Tests y script `node scripts/probar-clientes-demo.mjs`. El funcionamiento con bases reales queda pendiente de verificación posterior.
