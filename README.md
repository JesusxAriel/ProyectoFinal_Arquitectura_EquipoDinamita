# PuntoMarket · Microservicios (3EC)

Entregable 3EC de Arquitectura de Software, Grupo 2 (Equipo Dinamita). Frontend Razor → API Gateway (YARP) con Keycloak → 4 microservicios, cada uno con su propia base de datos. Todo corre en Docker. El módulo de Ventas queda fuera de este entregable.

```
Frontend (Razor) → Gateway (YARP) → usuarios-ms    → MySQL
                        │         → clientes-ms    → PostgreSQL
                        │         → productos-ms   → MySQL
                        │         → proveedores-ms → MongoDB
                   Keycloak (identidad, JWT)
```

## Estructura y responsables

| Carpeta | Contenido | Responsable |
|---|---|---|
| `Gateway/` | API Gateway oficial con YARP | Ari |
| `Frontend/` | Razor Pages oficial (`PuntoMarket.Frontend`) | Flora (pantallas), Ori (cambio de contraseña y primer inicio de sesión), Ari (`Auth`) |
| `Servicio1.Clientes/` | Clientes · Hexagonal · PostgreSQL | Felipe (Grupo 1) |
| `Servicio2.Usuarios/` | Usuarios · Clean · MySQL | Ale (Domain, Application, MySQL) y Ari (Keycloak, correo, Api) |
| `Servicio3.Productos/` | Productos · Hexagonal · MySQL | Leo (Grupo 1) |
| `Servicio4.Proveedores/` | Proveedores · Clean · MongoDB (implementado) | Ariel (Grupo 1) |
| `docker/` | Archivos de apoyo de Docker (el realm de Keycloak irá en `docker/keycloak/`) | Ari |
| `docker-compose.yml`, `.env.example` | Orquestación y nombres de variables | Ari |
| `Proyecto_Microservicios.slnx` | Solución de Ariel con Proveedores y los temporales | Ariel |
| `scripts/` | Pruebas JavaScript y smoke test de Proveedores | Ariel |

Documentos de integración de Ariel, que se conservan: `GUIA_INTEGRACION_MICROSERVICIOS.md`, `INTEGRACION_GRUPO2.md`, `PATRONES_Y_ARQUITECTURA.md` y `AGENTS.md`. Cada carpeta de servicio tiene su propio `README.md`.

### Carpetas temporales (de Ariel)

Sirven para ejecutar Proveedores sin Docker y se retirarán cuando la integración real esté lista. No forman parte del flujo oficial:

- `ApiGateway/ApiGateway.Demo/`: Gateway temporal. El oficial es `Gateway/`.
- `Frontend/Frontend.Demo/`: Frontend temporal de Proveedores. El oficial es `Frontend/` (su `.csproj` excluye esta carpeta).
- `DevIdentity/DevIdentity.Demo/`: emisor de JWT de demostración. Se reemplaza por Keycloak.

## Servicios y puertos

Dentro de Docker todos los servicios escuchan en el puerto 8080 y se llaman por su nombre: `gateway`, `keycloak`, `usuarios-ms`, `clientes-ms`, `productos-ms`, `proveedores-ms`.

| Servicio | Puerto en el equipo |
|---|---|
| Frontend | 5100 |
| Gateway | 5000 |
| Usuarios | 5003 |
| Keycloak | 8180 |
| MySQL de Usuarios | 3308 |

Los demás puertos publicados (Clientes, Productos, Proveedores y sus bases de datos) tienen un valor por defecto en `docker-compose.yml` y se pueden cambiar desde `.env`.

## Cómo levantar con Docker

El proyecto de Compose se llama `puntomarket-microservicios`.

1. Copiar la plantilla de variables y completarla. Las credenciales no se versionan.

   ```powershell
   Copy-Item .env.example .env
   ```

2. Construir y levantar todo.

   ```powershell
   docker compose up -d --build
   ```

3. Ver el estado.

   ```powershell
   docker compose ps
   ```

4. Comprobar la salud de cada servicio a través del Gateway: `http://localhost:5000/api/clientes/salud`, `/api/productos/salud`, `/api/proveedores/salud` y `/api/usuarios/salud`. El Gateway traduce cada ruta a la de salud real del servicio (`/api/salud`, o `/health` en Proveedores).

Detener sin borrar los datos:

```powershell
docker compose down
```

No usar `-v` salvo que se quiera borrar las bases de datos.

## Compilar y probar sin Docker

```powershell
dotnet build Gateway/PuntoMarket.Gateway.csproj
dotnet build Frontend/PuntoMarket.Frontend.csproj
dotnet build Servicio1.Clientes/PuntoMarket.Clientes.slnx
dotnet build Servicio2.Usuarios/PuntoMarket.Usuarios.slnx
dotnet build Servicio3.Productos/PuntoMarket.Productos.slnx
dotnet build Servicio4.Proveedores/Proveedores.slnx
dotnet test Servicio2.Usuarios/PuntoMarket.Usuarios.slnx
dotnet test Servicio4.Proveedores/Proveedores.slnx
```

Las pruebas de persistencia de Proveedores necesitan MongoDB real: se omiten si no existe la variable `PROVEEDORES_TEST_MONGO`. Para ejecutarlas:

```powershell
$env:PROVEEDORES_TEST_MONGO = 'mongodb://localhost:27017'
dotnet test Servicio4.Proveedores/Proveedores.slnx
```

Para ejecutar Proveedores con los temporales de Ariel (identidad de demostración, Gateway y Frontend de demo, MongoDB local), seguir `Servicio4.Proveedores/README.md` y `Frontend/Frontend.Demo/README.md`.

## Estado actual y pendientes

- **Keycloak** levanta en modo desarrollo sin realm. El realm `puntomarket` (roles, cliente, política de contraseña, administrador inicial) lo configura Ari.
- **Gateway** reenvía las rutas de los 4 servicios sin autenticación. Falta el login (`/api/auth/login`, `/api/auth/logout`) y la validación del token. No incluye la ruta temporal `/dev-auth` de Ariel.
- **Proveedores** está implementado (CRUD, Clean, MongoDB, JWT con roles). Por defecto valida tokens contra la identidad de demostración (`http://localhost:5300/realms/proveedores-dev`), que no existe en Docker: las rutas protegidas no funcionarán hasta apuntar `Authentication__Jwt__Authority` a Keycloak. `/health` es anónimo.
- **Clientes y Productos** son esqueletos de arquitectura sin lógica. Solo exponen `GET /api/salud`.
- **Usuarios** compila y pasa sus pruebas, pero falta adaptarlo a los contratos (`Result`, Keycloak, `EnviarCredenciales`), el caso de uso de crear usuario (fuera de la compilación), el script de base de datos y la validación de tokens.
- **Frontend**: las páginas `Pages/Account` y `Pages/Users` están fuera de la compilación hasta rehacerlas con los adaptadores del Gateway. Falta integrar el módulo de Proveedores de `Frontend.Demo` en el Frontend oficial.
- Las bases de datos de Clientes, Productos y Usuarios arrancan vacías. Proveedores crea su colección, sus índices y 7 proveedores de ejemplo al iniciar.
- Los nombres de las variables de conexión de Clientes y Productos (`ConnectionStrings__PostgresConnection`, `ConnectionStrings__MySqlConnection`) son una propuesta del `docker-compose.yml`. Los de Proveedores (`Mongo__ConnectionString`, `Mongo__DatabaseName`) son los que lee su código.
- La contraseña de MongoDB va dentro de una URI: si contiene caracteres especiales (`@`, `:`, `/`) hay que codificarla.
- Los documentos de Ariel todavía mencionan `Servicio1.Proveedores`; la carpeta ahora es `Servicio4.Proveedores`.

## Reglas del repositorio

- Nadie sube directo a `main`: cada tarea va en una rama `feat/<tema>` o `fix/<tema>` y se integra con pull request.
- Commits pequeños, en español, con prefijo (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`).
- Nunca `push --force` ni `git add .`: se agregan los archivos por nombre.
- Nada de secretos en el repositorio: solo se sube `.env.example`.
