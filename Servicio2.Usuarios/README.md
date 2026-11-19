# Servicio2.Usuarios

Microservicio de Usuarios. Arquitectura Clean con MySQL. Responsables: Ale (Domain, Application y persistencia MySQL) y Ari (Keycloak, correo y Api).

- `src/PuntoMarket.Usuarios.Domain`: entidades, servicios de dominio (nombre de usuario, política de contraseña) y `Result`.
- `src/PuntoMarket.Usuarios.Application`: puertos y casos de uso.
- `src/PuntoMarket.Usuarios.Infrastructure`: MySQL y envío de correo SMTP.
- `src/PuntoMarket.Usuarios.Api`: controladores HTTP.

Pendiente respecto a `guias/05_CONTRATOS.md`:

- Adaptar los puertos y los casos de uso a los contratos (`Result`, `IGestorDeIdentidad`, `IEnviadorCorreo.EnviarCredenciales`).
- `UseCases/Gestion/CrearUsuario/CrearUsuarioCasoDeUso.cs` quedó fuera de la compilación (`.csproj` de Application) porque dependía de BCrypt, tokens de activación y `UsuarioDuplicadoException`. Reescribirlo con `Result` y Keycloak.
- La Api no tiene validación de tokens ni endpoint `POST /api/usuarios`. Sí expone `GET /api/salud` anónimo.
- No hay script de base de datos propio del servicio.

```powershell
dotnet build Servicio2.Usuarios/PuntoMarket.Usuarios.slnx
dotnet test Servicio2.Usuarios/PuntoMarket.Usuarios.slnx
```
