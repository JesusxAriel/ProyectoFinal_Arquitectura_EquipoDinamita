# Frontend

Razor Pages (`PuntoMarket.Frontend`). Se comunica solo con el Gateway (`GatewayApi:BaseUrl`).

Copiado del proyecto anterior como esqueleto: `_Layout`, página de inicio, error, estilos, Bootstrap/jQuery y `Compartido/Result.cs`.

`Pages/Users` usa `IUsuariosApiAdapter` y los contratos del namespace `Usuarios`. Las acciones de edición y estado se envían por POST; los IDs de usuario no aparecen en la URL. La gestión no captura ni muestra contraseñas: el cambio se realiza desde las páginas existentes de `/Auth`.

Pendiente: revisar la ortografía de todos los textos de la interfaz.

```powershell
dotnet build Frontend/PuntoMarket.Frontend.csproj
```
