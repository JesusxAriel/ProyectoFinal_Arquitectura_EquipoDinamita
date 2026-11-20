# SOLID y Clean Code

- S: `ClienteValidador` solo valida datos; `MySqlClienteRepository` solo persiste.
- O: agregar un creador de cliente no exige modificar entidad o selector mediante nuevas clases.
- L: `ClienteCreator` define el contrato reemplazable de sus creadores.
- I: los puertos separan listado, lectura, creación, actualización, baja y reactivación.
- D: Application depende de `IClienteRepository`, no de MySQL.

Si cambia MySQL por otra base, se crea un nuevo adaptador que implementa `IClienteRepository`; el Domain y Application no cambian.
