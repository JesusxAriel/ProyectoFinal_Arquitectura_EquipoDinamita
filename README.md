# PuntoMarket — Grupo 1: Proveedores

Microservicio de proveedores para un micromercado con .NET 10, Clean Architecture y MongoDB, acompañado de frontend Razor Pages, Gateway YARP e identidad de desarrollo.

**Alcance: micro-proveedor.** Grupo 1 mantiene Proveedores. Grupo 2 desarrolla Usuarios, identidad/Keycloak, Gateway y sesión comunes en otras referencias; no se activan automáticamente aquí. [Estado y hashes revisados](INTEGRACION_GRUPO2.md).

## Estructura activa

```text
Proyecto_Microservicios.slnx
Servicio4.Proveedores/        Domain, Application, Infrastructure, API y Tests
ApiGateway/ApiGateway.Demo/   Gateway de esta rama
Frontend/Frontend.Demo/       PuntoMarket y login demo
DevIdentity/DevIdentity.Demo/  Emisor temporal RS256
scripts/                     Pruebas y verificación Docker
```

Servicio4.Proveedores es la única ubicación activa del microservicio en esta rama. El traslado conserva sus proyectos, pruebas y namespaces; ver [diagnóstico y alcance](GUIA_INTEGRACION_GRUPO1.md#1-diagnóstico-de-servicio1-y-servicio4).

## Docker

Requiere Docker Desktop abierto con motor Linux y Compose. Desde la raíz:

```powershell
# Opcional: copiar solo si todavía no existe .env y se desean cambiar los puertos.
if (-not (Test-Path -LiteralPath .env)) { Copy-Item .env.example .env }
docker compose up --build -d
docker compose ps
```

Predeterminados: frontend http://localhost:5100, Gateway :5000, API :5200, DevIdentity :5300 y MongoDB Docker :27018. Las variables de terminal/.env pueden cambiar puertos efectivos. Son cinco servicios y un seed de siete proveedores; no se utiliza ni copia MongoDB de Windows. down conserva volúmenes; no usar down -v para reiniciar.

Seguir [GUIA_DOCKER_PUNTOMARKET.md](GUIA_DOCKER_PUNTOMARKET.md) para requisitos, variables, Compass y persistencia.

## Local sin Docker

Con .NET 10 SDK y MongoDB local en 27017, en terminales distintas:

```powershell
dotnet run --project DevIdentity/DevIdentity.Demo
dotnet run --project Servicio4.Proveedores/Proveedores.API
dotnet run --project ApiGateway/ApiGateway.Demo
dotnet run --project Frontend/Frontend.Demo
```

No usar los mismos puertos a la vez con Docker. En /Login elegir Administrador (CRUD) o Cajero (consulta). Es demo sin credenciales personales: la API sí valida JWT/roles, pero no es identidad de producción.

## Guías y pruebas

- [Grupo 1: contrato, arquitectura, reglas, patrones y consumo](GUIA_INTEGRACION_GRUPO1.md).
- [Grupo 2: integrado aquí, presente en otras ramas y pendiente](INTEGRACION_GRUPO2.md).
- [Docker paso a paso](GUIA_DOCKER_PUNTOMARKET.md).
- [API breve](Servicio4.Proveedores/README.md) y [frontend](Frontend/Frontend.Demo/README.md).

```powershell
dotnet test Servicio4.Proveedores/Proveedores.slnx
node scripts/probar-validaciones-frontend.cjs
node scripts/probar-busqueda-dinamica.mjs
```

Sin PROVEEDORES_TEST_MONGO se omiten pruebas Mongo; con servidor de pruebas se usan bases temporales aisladas. No versionar datos personales, tokens, claves privadas ni .env local.

Pendientes: integración acordada de las variantes Grupo 2 y HTTP de Productos antes de impedir bajas con productos activos. Proveedores permanece independiente; no se declara ejecutado aquí lo presente solo en otras referencias.
