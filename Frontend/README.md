# Frontend

Razor Pages (`PuntoMarket.Frontend`). Se comunica solo con el Gateway (`GatewayApi:BaseUrl`).

Copiado del proyecto anterior como esqueleto: `_Layout`, página de inicio, error, estilos, Bootstrap/jQuery y `Compartido/Result.cs`.

Pendiente:

- `Pages/Account/**` y `Pages/Users/**` están en el repositorio pero fuera de la compilación (`.csproj`): dependían de clientes HTTP y de la sesión por JWT propio del proyecto anterior. Se rehacen con los adaptadores (`AuthApiAdapter`, `UsuariosApiAdapter`) y `Result`.
- Revisar la ortografía de todos los textos de la interfaz.

```powershell
dotnet build Frontend/PuntoMarket.Frontend.csproj
```
