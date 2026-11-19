# Gateway

API Gateway con YARP (`Yarp.ReverseProxy`). Rutas y clusters en `appsettings.json` (sección `ReverseProxy`), hacia los servicios por nombre de Docker en el puerto interno 8080.

| Ruta | Destino |
|---|---|
| `/api/clientes/**` | `http://clientes-ms:8080/` |
| `/api/productos/**` | `http://productos-ms:8080/` |
| `/api/proveedores/**` | `http://proveedores-ms:8080/` |
| `/api/usuarios/**` | `http://usuarios-ms:8080/` |

Pendiente (Ari): autenticación con Keycloak, políticas por rol y `POST /api/auth/login` y `/api/auth/logout`.

```powershell
dotnet build Gateway/PuntoMarket.Gateway.csproj
```
