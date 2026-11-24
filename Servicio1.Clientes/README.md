# Servicio1.Clientes

Microservicio de Clientes de PuntoMarket. **Arquitectura Hexagonal** con **MySQL 8.4** (Grupo 1).

## Capas (puertos y adaptadores)

| Proyecto | Rol |
|---|---|
| `PuntoMarket.Clientes.Domain` | Entidad `Cliente`, validaciones, `Result`/`Error`, creadores (Factory Method). Sin dependencias. |
| `PuntoMarket.Clientes.Application` | Puertos de entrada (`Ports/In`: casos de uso), puertos de salida (`Ports/Out`: `IClienteRepository`, `ICurrentUser`), casos de uso y DTOs. |
| `PuntoMarket.Clientes.Adapters.Out.Persistence` | Adaptador de salida: `MySqlClienteRepository` (Adapter), `MySqlDataSource` como Singleton por DI, inicializador de tabla y datos iniciales. |
| `PuntoMarket.Clientes.Adapters.In.Api` | Adaptador de entrada HTTP: controlador, JWT/Keycloak, `ResultHttpMapper`. Raíz de composición. |
| `tests/PuntoMarket.Clientes.Tests` | Pruebas unitarias de dominio y casos de uso (sin base de datos). |

Dependencias: `Api -> Application + Persistence`, `Persistence -> Application`, `Application -> Domain`. El dominio no conoce MySQL ni HTTP.

## Patrones de diseño

| Patrón | Dónde |
|---|---|
| Result | `Domain/Common/Result.cs`; toda validación esperada devuelve `Result`, sin excepciones. `ResultHttpMapper` lo traduce a HTTP. |
| Adapter | `MySqlClienteRepository` adapta el puerto `IClienteRepository` a MySQL. |
| Factory Method | `ClienteCreator` (+ `ClienteEstandarCreator`, `ClienteGenericoCreator`, `ClienteCreatorSelector`). |
| Singleton | `MySqlDataSource` registrado con `AddSingleton` en `Persistence/DependencyInjection.cs`. |

## Endpoints (por el Gateway: `/api/clientes`)

| Método | Ruta | Rol | Éxito |
|---|---|---|---|
| GET | `/api/clientes` | Administrador, Cajero | 200 página |
| GET | `/api/clientes/{id}` | Administrador, Cajero | 200 |
| POST | `/api/clientes` | Administrador, Cajero | 201 |
| PUT | `/api/clientes/{id}` | Administrador, Cajero | 200 |
| DELETE | `/api/clientes/{id}` | Administrador | 204 (baja lógica) |
| PATCH | `/api/clientes/{id}/reactivar` | Administrador | 200 |
| GET | `/api/salud` | Anónimo | 200 |

Parámetros del listado: `busqueda`, `estado` (Todos/Activos/Inactivos), `ordenarPor` (RazonSocial/NitCi/FechaCreacion/FechaActualizacion), `descendente`, `pagina`, `tamanoPagina` (1–100).
Cuerpo de POST/PUT: `{ "nitCi": "...", "razonSocial": "...", "correo": null, "telefono": null }`.
Errores: `ProblemDetails` con `code` y, en validación, `errors` por campo (400); 404 inexistente; 409 NIT/CI duplicado, cliente inactivo o genérico protegido.

## Reglas de negocio

- NIT/CI: 5–15 dígitos, único **entre clientes activos** (columna virtual `nitCiActivo` + índice único). El NIT `0` está reservado al cliente genérico "Sin Nombre".
- El cliente genérico no se edita ni se da de baja. No se edita un cliente inactivo. No se reactiva si otro activo usa el mismo NIT/CI.
- Razón social obligatoria (≤150), correo opcional válido, teléfono opcional de 8 dígitos que empieza con 4, 6 o 7 (misma política que Proveedores).
- Baja lógica: nunca se borra la fila. Auditoría (`idUsuarioCreacion`, `idUsuarioActualizacion`, fechas UTC) la fija el servidor con el claim `sub`; no hay FK a la tabla de Usuarios (otra base).

## Base de datos

`database/clientes_mysql.sql` (idéntico al que ejecuta el inicializador). Al iniciar, la API crea la tabla si no existe y, **solo si está vacía**, inserta el cliente genérico y dos clientes de ejemplo (`Clientes:SeedEnabled=false` lo desactiva; `Clientes:InitializeOnStartup=false` desactiva todo).

## Ejecución

Requiere .NET 10 SDK. Cadena de conexión por variable de entorno (no se versiona):

```powershell
$env:ConnectionStrings__MySqlConnection = "Server=localhost;Port=3310;Database=<db>;User ID=<usuario>;Password=<clave>"
dotnet test Servicio1.Clientes/PuntoMarket.Clientes.slnx
dotnet run --project Servicio1.Clientes/src/PuntoMarket.Clientes.Adapters.In.Api
```

En el entorno común: `docker compose up --build -d clientes-ms` (define `CLIENTES_DB_*` y `CLIENTES_DB_ROOT_PASSWORD` en `.env`). El JWT se valida con audiencia `clientes-api` (mapper ya presente en el realm de Keycloak).

## Seed selectivo de Clientes

`Clientes:SeedGenericoEnabled` vale `true` por defecto y conserva las semillas históricas de otros entornos. Al configurarlo como `false` y mantener `Clientes:SeedEnabled=true`, en una tabla vacía se insertan los dos clientes normales. Una tabla con registros no se modifica ni recibe semillas adicionales al reiniciar. Esta opción no borra ni oculta un genérico existente; las protecciones y el contrato `esGenerico` se conservan por compatibilidad. Antes de una retirada manual, verificar copia de seguridad y referencias históricas o externas (Ventas), sin cambiar identificadores ni volúmenes.
