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
- `db/init/` contiene el esquema (`ROL`, `USUARIO` y los dos roles) y la siembra del administrador
  inicial a partir de `PUNTOMARKET_ADMIN_USERNAME`, `PUNTOMARKET_ADMIN_EMAIL` y
  `PUNTOMARKET_ADMIN_NOMBRE`. Falta que `usuarios-db` monte esa carpeta en
  `/docker-entrypoint-initdb.d` y reciba esas variables; MySQL solo ejecuta los scripts cuando la base
  aún no existe, por lo que en un volumen ya creado hay que aplicarlos a mano.

```powershell
dotnet build Servicio2.Usuarios/PuntoMarket.Usuarios.slnx
dotnet test Servicio2.Usuarios/PuntoMarket.Usuarios.slnx
```
