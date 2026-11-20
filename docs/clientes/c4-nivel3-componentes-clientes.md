# C4 nivel 3: componentes de Clientes

```mermaid
flowchart LR
    Controller[ClientesController] --> UseCases[Casos de uso]
    UseCases --> PortsIn[Puertos de entrada]
    UseCases --> PortOut[IClienteRepository]
    PortOut --> Adapter[MySqlClienteRepository]
    Adapter --> MySQL[(MySQL)]
    Security[JWT y políticas] --> Controller
```

Archivos principales: `src/PuntoMarket.Clientes.Adapters.In.Api/Controllers/ClientesController.cs`, `src/PuntoMarket.Clientes.Application/UseCases/ClienteUseCases.cs`, `src/PuntoMarket.Clientes.Application/Ports/Out/IClienteRepository.cs` y `src/PuntoMarket.Clientes.Adapters.Out.Persistence/Repositories/MySqlClienteRepository.cs`.
