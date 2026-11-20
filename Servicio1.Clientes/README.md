# Servicio1.Clientes

Microservicio de Clientes. Arquitectura Hexagonal con MySQL 8.4.

- Domain: entidad, validaciones, creadores y Result.
- Application: puertos de entrada/salida y casos de uso.
- Persistence: adaptador MySQL mediante MySqlConnector.
- API: controlador, autenticación JWT/Keycloak y políticas de roles.

Base independiente `CLIENTE`, sin claves foráneas hacia otros microservicios.
