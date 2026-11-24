# Servicio2.Usuarios

Microservicio de Usuarios. Arquitectura Clean con MySQL. Responsables: Ale (Domain, Application y persistencia MySQL) y Ari (Keycloak, correo y Api).

- `src/PuntoMarket.Usuarios.Domain`: entidades, `Result`, validaciones reutilizables y servicios de dominio (protocolo de nombre de usuario, política de contraseña y generación de una contraseña aleatoria desconocida).
- `src/PuntoMarket.Usuarios.Application`: puertos y un caso de uso por clase, con Entrada y Salida.
- `src/PuntoMarket.Usuarios.Infrastructure`: MySQL (ADO.NET con consultas parametrizadas), adaptador de Keycloak y envío de correo SMTP.
- `src/PuntoMarket.Usuarios.Api`: controladores HTTP y validación del JWT de Keycloak.

La contraseña no se guarda en la tabla `USUARIO`: vive solo en Keycloak. La cuenta se activa con un enlace de un solo uso enviado por correo: el usuario define su contraseña en una pantalla propia y `debeCambiarPassword` pasa a falso. Detalle del contrato en [ACTIVACION.md](ACTIVACION.md).

## Patrones

- **Result**: las validaciones de negocio devuelven `Result`/`Result<T>` y nunca lanzan excepciones; las excepciones quedan para fallos técnicos.
- **Factory Method**: `Usuario.Crear(...)` devuelve `Result<Usuario>`, con constructor privado, así que un usuario inválido no llega a existir.
- **Singleton**: `ConexionMySql` se registra con `AddSingleton` y construye la cadena de conexión una sola vez con `Lazy<T>`.
- **Adapter**: `KeycloakGestorDeIdentidad` y `SmtpEnviadorCorreo` implementan puertos de Application.

## Rutas

| Ruta | Método | Acceso |
|---|---|---|
| `/api/usuarios` | GET, POST | Administrador |
| `/api/usuarios/{id}` | GET, PUT | Administrador |
| `/api/usuarios/{id}/estado` | PATCH | Administrador |
| `/api/usuarios/{id}/reenviar-acceso` | POST | Administrador |
| `/api/activacion` | POST | Anónimo (token de un solo uso) |
| `/api/usuarios/roles` | GET | Administrador |
| `/api/usuarios/perfil` | GET | Cualquier usuario autenticado |
| `/api/usuarios/perfil/password` | PUT | Cualquier usuario autenticado |
| `/api/salud` | GET | Anónimo |

`GET /api/usuarios` devuelve un arreglo de `UsuarioResponse`, como pide `05_CONTRATOS.md`. Los errores de negocio salen como `400 { "error": "mensaje en español" }`; un identificador que no existe devuelve `404` con la misma forma, y `GET /api/usuarios/roles` se conserva para el combo de roles del Razor aunque no esté en la tabla de rutas del contrato.

## Actualización de un usuario

`PUT /api/usuarios/{id}` cambia el nombre completo, el correo y el rol **solo en nuestra tabla**: esos datos no se propagan a Keycloak, porque el puerto `IGestorDeIdentidad` del contrato no tiene una operación para editarlos. Si la entrega necesita que el correo también cambie en Keycloak, hay que acordar con Ari agregar esa operación al puerto y a su adaptador.

El `username` **no cambia nunca**. Se genera una sola vez al crear el usuario, con el protocolo del dominio (inicial del primer nombre más el primer apellido, en minúsculas y sin tildes, numerado si ya existe), y es la clave con la que Keycloak, el login del Gateway y las columnas de auditoría identifican a la persona.

## Pendiente

- `db/init/` contiene el esquema (`ROL`, `USUARIO` y los dos roles) y la siembra del administrador inicial a partir de `PUNTOMARKET_ADMIN_USERNAME`, `PUNTOMARKET_ADMIN_EMAIL` y `PUNTOMARKET_ADMIN_NOMBRE`. MySQL solo ejecuta esos scripts cuando la base aún no existe, por lo que en un volumen ya creado hay que aplicarlos a mano.

```powershell
dotnet build Servicio2.Usuarios/PuntoMarket.Usuarios.slnx
dotnet test Servicio2.Usuarios/PuntoMarket.Usuarios.slnx
```
