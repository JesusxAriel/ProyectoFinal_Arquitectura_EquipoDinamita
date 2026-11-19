> Integración actual: el entorno común utiliza el Compose raíz, Gateway/, Frontend/ y Keycloak. Los comandos y componentes demo descritos aquí pertenecen exclusivamente a `docker-compose.demo.yml`. Los resultados de auditorías anteriores son históricos; véase el README y los acuerdos Grupo 2 para el estado integrado.

# Microservicio Proveedores

Servicio independiente .NET 10 con Clean Architecture y MongoDB. Arranque y requisitos completos en el [README principal](../README.md).

| Proyecto | Responsabilidad |
|---|---|
| Proveedores.Domain | Entidad, reglas, Result y Factory Method; sin MongoDB ni HTTP |
| Proveedores.Application | Casos de uso, DTOs y puertos |
| Proveedores.Infrastructure | Adaptadores MongoDB/Productos, seed, índices y Singleton de MongoClient |
| Proveedores.API | JWT, roles, endpoints y traducción Result a HTTP |
| Proveedores.Tests | Pruebas unitarias, HTTP, frontend y persistencia temporal |

## API

| Método | Ruta | Permiso | Éxito |
|---|---|---|---|
| GET | /api/proveedores | Administrador/Cajero | 200 |
| GET | /api/proveedores/{id} | Administrador/Cajero | 200 |
| POST | /api/proveedores | Administrador | 201 |
| PUT | /api/proveedores/{id} | Administrador | 200 |
| DELETE | /api/proveedores/{id} | Administrador | 204, baja lógica |
| PATCH | /api/proveedores/{id}/reactivar | Administrador | 200 |

Errores: 400 validación, 401 autenticación, 403 permisos, 404 ausencia/ID malformado, 409 conflicto y 503 integración no disponible. ProblemDetails incluye code/detail y errores por campo cuando corresponde.

Listado: busqueda, estado (Todos/Activos/Inactivos), ordenarPor (Nombre/Numero/FechaCreacion/FechaActualizacion), descendente, pagina y tamanoPagina (máximo 100). La UI limita el orden visible a nombre A-Z/Z-A. Mongo filtra y ordena antes de paginar, con _id como desempate; durante búsqueda prioriza prefijo de nombre, parcial de nombre y después teléfono/correo.

## Persistencia y reglas

Base predeterminada proveedores_db, colección proveedores; conexión local sin autenticación mongodb://localhost:27017. Configuración Mongo:* mediante appsettings o variables Mongo__*. No publicar credenciales de otras instalaciones.

El índice único parcial impide nombres normalizados repetidos entre activos. No se editan inactivos; las bajas conservan el documento. El seed crea siete proveedores en una base vacía mediante $setOnInsert y no sobrescribe registros ya sembrados.

Auditoría del servidor: creador, último modificador y fechas UTC. version se incrementa atómicamente junto con la actualización y documentos sin ese campo equivalen a 0. Un conflicto devuelve 409; no se exige version en el DTO.

Validación definitiva en Domain: nombre obligatorio de hasta 150 caracteres, teléfono nacional string de 8 dígitos comenzando por 4/6/7 sin prefijo, correo opcional de hasta 150. Los datos comerciales no aceptan HTML ni controles peligrosos.

## Integración y pruebas

[Guía común](../GUIA_INTEGRACION_GRUPO1.md) y [acuerdos Grupo 2](../INTEGRACION_GRUPO2.md). La API valida JWT configurado en Authentication:Jwt; no gestiona identidades ni consulta la base de Usuarios. Productos continúa pendiente de integración HTTP; no se bloquean todavía las bajas por productos activos.

```powershell
$env:PROVEEDORES_TEST_MONGO = 'mongodb://localhost:27017'
dotnet test Servicio4.Proveedores/Proveedores.slnx
```

Ejecutar desde la raíz. Las pruebas de MongoDB utilizan bases temporales aisladas. MongoSeedApiTests verifica API, seed inicial y reinicio sin duplicaciones. Docker está configurado aquí; Keycloak real permanece pendiente de integrar desde las referencias comunes.
