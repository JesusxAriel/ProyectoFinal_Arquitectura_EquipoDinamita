# Servicio3.Productos

Microservicio de Productos. Arquitectura Hexagonal con PostgreSQL. Responsable: Leo (Grupo 1).

- `src/PuntoMarket.Productos.Domain`: entidades, validaciones y `Result`.
- `src/PuntoMarket.Productos.Application`: puertos de entrada (`Ports/In`), puertos de salida (`Ports/Out`) y casos de uso.
- `src/PuntoMarket.Productos.Adapters.Out.Persistence`: adaptador de salida hacia PostgreSQL.
- `src/PuntoMarket.Productos.Adapters.In.Api`: adaptador de entrada HTTP.

Estado: esqueleto sin lógica de negocio. Solo responde `GET /api/salud`.

```powershell
dotnet build Servicio3.Productos/PuntoMarket.Productos.slnx
```
