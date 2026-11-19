# Servicio1.Clientes

Microservicio de Clientes. Arquitectura Hexagonal con PostgreSQL. Responsable: Felipe (Grupo 1).

- `src/PuntoMarket.Clientes.Domain`: entidades, validaciones y `Result`.
- `src/PuntoMarket.Clientes.Application`: puertos de entrada (`Ports/In`), puertos de salida (`Ports/Out`) y casos de uso.
- `src/PuntoMarket.Clientes.Adapters.Out.Persistence`: adaptador de salida hacia PostgreSQL.
- `src/PuntoMarket.Clientes.Adapters.In.Api`: adaptador de entrada HTTP.

Estado: esqueleto sin lógica de negocio. Solo responde `GET /api/salud`.

```powershell
dotnet build Servicio1.Clientes/PuntoMarket.Clientes.slnx
```
