# Diagrama de clases

```mermaid
classDiagram
    class Cliente
    class ClienteValidador
    class ClienteCreator
    class ClienteEstandarCreator
    class ClienteGenericoCreator
    class IClienteRepository
    class MySqlClienteRepository
    class ClienteUseCases
    class Result
    ClienteCreator <|-- ClienteEstandarCreator
    ClienteCreator <|-- ClienteGenericoCreator
    IClienteRepository <|.. MySqlClienteRepository
```

Patrones: `Result` en `Domain/Common/Result.cs`, Factory Method en `Domain/Creators/ClienteCreator.cs`, Adapter en `Application/Ports/Out/IClienteRepository.cs` y `Adapters.Out.Persistence/Repositories/MySqlClienteRepository.cs`, Singleton para `MySqlDataSource` en `Adapters.Out.Persistence/DependencyInjection.cs`.
