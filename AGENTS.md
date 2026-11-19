# AGENTS.md — guía para agentes de IA

1. Lea primero `README.md` e `INTEGRACION_GRUPO2.md`.
2. Respete Clean Architecture: Domain no depende de nada; Application solo de Domain; Infrastructure de Application; API compone todo.
3. No agregue paquetes externos ni referencias a Domain (nada de MongoDB, ASP.NET, HTTP).
4. No sustituya `Result` por excepciones para validaciones de negocio; las excepciones son para fallos técnicos.
5. No elimine los cuatro patrones: Result, Adapter (Mongo y Productos), Factory Method (`ProveedorCreator`), Singleton (`MongoClient` vía DI).
6. No conecte bases de datos de distintos microservicios; la integración entre servicios va por HTTP y adaptadores.
7. No introduzca autenticación ficticia en producción. `DevIdentity.Demo`, el selector de perfil del frontend y el Gateway son temporales y están marcados con `DEMO TEMPORAL` / `GATEWAY TEMPORAL`.
8. No cambie contratos públicos (rutas, DTOs, códigos HTTP) sin documentarlo en `INTEGRACION_GRUPO2.md` y el README.
9. Ejecute `dotnet test Servicio4.Proveedores/Proveedores.slnx` tras cada modificación y no declare éxito sin ejecutarlo.
10. Mantenga el microservicio independiente: debe funcionar sin Productos u otros CRUD.
11. Documente cualquier cambio de integración (configuración, claims, rutas, variables).

Se permiten mejoras legítimas y corrección de errores que respeten lo anterior.
