# PuntoMarket

Sistema de punto de venta para un minimarket, construido como microservicios sobre .NET 10. Un frontend Razor Pages se comunica únicamente con un Gateway, que valida el JWT emitido por Keycloak y enruta hacia cuatro microservicios independientes, cada uno con su propia base de datos.

## Arquitectura

```text
Navegador ──► Frontend (Razor Pages, sesión)
                  │  HTTP + JWT
                  ▼
              Gateway (YARP) ──► Keycloak (realm puntomarket)
                  │
   ┌──────────────┼───────────────┬────────────────┐
   ▼              ▼               ▼                ▼
Clientes       Usuarios       Productos       Proveedores
MySQL 8.4      MySQL 8.4      PostgreSQL 17   MongoDB 8
```

| Componente | Responsabilidad | Arquitectura |
|---|---|---|
| Frontend | Interfaz Razor Pages con sesión de servidor; consume solo el Gateway mediante adaptadores HTTP | Razor Pages, adaptadores y `Result` |
| Gateway | Rutas YARP, validación del JWT de Keycloak, políticas por rol, `POST /api/auth/login` y `POST /api/auth/logout` | YARP |
| Keycloak 26 | Identidad y roles (`Administrador`, `Cajero`); importa el realm `puntomarket` al arrancar | Realm versionado en `docker/keycloak` |
| Clientes | Directorio de clientes con baja lógica | Hexagonal (puertos y adaptadores), MySQL |
| Usuarios | Gestión de usuarios, protocolo de nombre de usuario, contraseña temporal, correo SMTP y sincronización con Keycloak | Clean Architecture, MySQL |
| Productos | Catálogo, categorías, empaques e historial de precios | Hexagonal, PostgreSQL |
| Proveedores | Directorio de proveedores con baja lógica | Clean Architecture, MongoDB |

Cada microservicio valida por sí mismo el JWT (firma, emisor, audiencia y vigencia) y aplica sus permisos. Las bases de datos no se comparten: la integración entre servicios va solo por HTTP. Las validaciones de negocio devuelven `Result` y no lanzan excepciones; las excepciones quedan para fallos técnicos.

Patrones aplicados en los servicios: Result, Adapter, Factory Method y Singleton por inyección de dependencias (por ejemplo, el cliente de base de datos).

## Estructura de carpetas

```text
Frontend/                 Razor Pages (PuntoMarket.Frontend)
Frontend.Tests/           Pruebas del frontend
Gateway/                  Gateway YARP y login contra Keycloak
Gateway.Tests/            Pruebas del Gateway
Servicio1.Clientes/       Domain, Application, Adapters.In.Api, Adapters.Out.Persistence y tests
Servicio2.Usuarios/       Domain, Application, Infrastructure, Api y tests
Servicio3.Productos/      Domain, Application, Adapters.In.Api, Adapters.Out.Persistence y tests
Servicio4.Proveedores/    Domain, Application, Infrastructure, API y Tests
docker/keycloak/          Realm de Keycloak importado al arrancar
docker-compose.yml        Los once contenedores del sistema
.env.example              Plantilla de variables de entorno
Proyecto_Microservicios.slnx   Solución con todos los proyectos
```

## Requisitos

- .NET SDK 10
- Docker Desktop con motor Linux y Compose

## Variables de entorno

Copie la plantilla y complete los valores. El archivo `.env` es privado y no se versiona.

```powershell
Copy-Item .env.example .env
```

| Grupo | Variables |
|---|---|
| Keycloak | `KEYCLOAK_ADMIN_USER`, `KEYCLOAK_ADMIN_PASSWORD` |
| Administrador inicial | `PUNTOMARKET_ADMIN_USERNAME`, `PUNTOMARKET_ADMIN_PASSWORD`, `PUNTOMARKET_ADMIN_EMAIL`, `PUNTOMARKET_ADMIN_NOMBRE` |
| Base de Usuarios | `USUARIOS_DB_NAME`, `USUARIOS_DB_USER`, `USUARIOS_DB_PASSWORD`, `USUARIOS_DB_ROOT_PASSWORD` |
| Base de Productos | `PRODUCTOS_DB_NAME`, `PRODUCTOS_DB_USER`, `PRODUCTOS_DB_PASSWORD` |
| Base de Clientes | `CLIENTES_DB_NAME`, `CLIENTES_DB_USER`, `CLIENTES_DB_PASSWORD`, `CLIENTES_DB_ROOT_PASSWORD` |
| Base de Proveedores | `PROVEEDORES_DB_NAME`, `PROVEEDORES_DB_USER`, `PROVEEDORES_DB_PASSWORD` |
| Correo SMTP (opcional) | `SMTP_HOST`, `SMTP_PORT`, `SMTP_ENABLE_SSL`, `SMTP_FROM_ADDRESS`, `SMTP_FROM_NAME`, `SMTP_USER_NAME`, `SMTP_PASSWORD` |
| Puertos publicados (opcionales) | `KEYCLOAK_PUBLISHED_PORT`, `GATEWAY_PUBLISHED_PORT`, `FRONTEND_PUBLISHED_PORT`, `USUARIOS_PUBLISHED_PORT`, `CLIENTES_PUBLISHED_PORT`, `PRODUCTOS_PUBLISHED_PORT`, `PROVEEDORES_PUBLISHED_PORT` y los `*_DB_PUBLISHED_PORT` de cada base |

Reglas importantes:

- `PUNTOMARKET_ADMIN_USERNAME` es el mismo valor para Keycloak y para Usuarios; el administrador inicial se crea en ambos.
- `PUNTOMARKET_ADMIN_PASSWORD` debe cumplir la política: mínimo 8 caracteres, con mayúscula, minúscula, número y carácter especial.
- El SMTP es opcional. Con las variables vacías el servicio arranca igual: el alta de un usuario responde 201 con `correoEnviado=false` y se puede usar el reenvío de acceso cuando haya correo configurado.

## Cómo levantar el sistema

```powershell
docker compose config --quiet
docker compose up --build -d
docker compose ps
```

Espere a que los once contenedores estén `healthy`. Los servicios esperan a sus bases de datos y a Keycloak antes de iniciar.

Para detener el sistema conservando contenedores y volúmenes:

```powershell
docker compose stop
```

No use `docker compose down -v`: elimina los volúmenes y con ellos los datos de todas las bases.

## Puertos

Los puertos del host son configurables con las variables `*_PUBLISHED_PORT`. Valores predeterminados:

| Servicio | Puerto | Servicio | Puerto |
|---|---:|---|---:|
| Frontend | 5100 | MySQL Usuarios | 3308 |
| Gateway | 5000 | MySQL Clientes | 3310 |
| Keycloak | 8180 | PostgreSQL Productos | 5433 |
| Usuarios | 5003 | MongoDB Proveedores | 27018 |
| Clientes | 5001 | Productos | 5004 |
| Proveedores | 5005 | | |

Dentro de la red de Docker los servicios se resuelven por nombre y escuchan en el puerto 8080.

## Usuarios y roles

Al iniciar por primera vez se crea el administrador definido en `PUNTOMARKET_ADMIN_*`. Desde `http://localhost:5100/Auth/Login` inicie sesión con ese usuario y cree los demás desde la pantalla Usuarios.

| Rol | Permisos |
|---|---|
| Administrador | Gestión de usuarios (alta, edición, baja lógica y reenvío de acceso); lectura y escritura en Clientes, Productos y Proveedores |
| Cajero | Lectura de Clientes, Productos y Proveedores; alta y edición de clientes; sin acceso a la gestión de usuarios |

Funcionamiento de las cuentas:

- El nombre de usuario se genera a partir del nombre y apellido (`jperez`, `jperez2`, ...).
- La contraseña temporal se genera automáticamente, se fija en Keycloak y se envía por correo. La contraseña no se guarda en la base de Usuarios.
- En el primer inicio de sesión el usuario debe cambiar su contraseña.
- El administrador puede desactivar y reactivar usuarios, y reenviar el acceso de un usuario activo con `POST /api/usuarios/{id}/reenviar-acceso`.
- Las tablas de Usuarios guardan auditoría con el nombre de usuario del token: usuario y fecha de creación, y usuario y fecha de la última modificación.

Rutas principales del Gateway:

| Ruta | Acceso |
|---|---|
| `POST /api/auth/login`, `POST /api/auth/logout` | Anónimo |
| `/api/usuarios/**` | Administrador |
| `GET /api/usuarios/perfil`, `PUT /api/usuarios/perfil/password` | Cualquier usuario autenticado |
| `/api/clientes/**`, `/api/productos/**`, `/api/proveedores/**` | Token válido; cada servicio aplica el rol por operación |
| `/api/*/salud` | Anónimo |

## Pruebas

```powershell
dotnet restore Proyecto_Microservicios.slnx
dotnet build Proyecto_Microservicios.slnx --no-restore
dotnet test Proyecto_Microservicios.slnx --no-restore
```

Las pruebas de persistencia de Proveedores contra MongoDB se omiten salvo que se defina una instancia de pruebas; usan bases temporales que eliminan al terminar:

```powershell
$env:PROVEEDORES_TEST_MONGO = 'mongodb://localhost:27017'
dotnet test Servicio4.Proveedores/Proveedores.slnx
```

## Documentación por componente

- [Gateway](Gateway/README.md)
- [Frontend](Frontend/README.md)
- [Clientes](Servicio1.Clientes/README.md)
- [Usuarios](Servicio2.Usuarios/README.md)
- [Productos](Servicio3.Productos/README.md)
- [Proveedores](Servicio4.Proveedores/README.md)

Nunca publique contraseñas, tokens ni archivos `.env`.
