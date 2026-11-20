# Hexagonal: puertos y adaptadores

- Puerto de entrada: interfaces en `Application/Ports/In/CasosDeUso.cs`.
- Puerto de salida: `IClienteRepository` e `ICurrentUser` en `Application/Ports/Out/IClienteRepository.cs`.
- Adaptador de entrada: `ClientesController` y seguridad JWT.
- Adaptador de salida: `MySqlClienteRepository`.
- Composición: `Program.cs` conecta puertos e implementaciones.
