> Integración actual: el entorno común utiliza el Compose raíz, Gateway/, Frontend/ y Keycloak. Los comandos y componentes demo descritos aquí pertenecen exclusivamente a `docker-compose.demo.yml`. Los resultados de auditorías anteriores son históricos; véase el README y los acuerdos Grupo 2 para el estado integrado.

# Frontend PuntoMarket

ASP.NET Core Razor Pages .NET 10. Instrucciones completas en el [README principal](../../README.md).

- /Login: acceso de demostración por perfil.
- /Index y /: panel con totales consultados desde Proveedores.
- /Proveedores: activos, eliminados, búsqueda, paginación, creación/edición en modales y reactivación.
- /Logout: cierre de sesión por POST con antiforgery.

Administrador escribe; Cajero consulta. Las restricciones definitivas se comprueban en la API. El frontend se comunica mediante el cliente nombrado ApiGateway; nunca accede a MongoDB ni a las APIs internas directamente.

## Ejecutar

Con MongoDB, DevIdentity, API y Gateway disponibles según el README principal:

```powershell
dotnet run --project Frontend/Frontend.Demo
```

Abrir http://localhost:5100/Login. ApiGateway:BaseUrl puede configurarse mediante ApiGateway__BaseUrl. Estilos, fuentes e iconos se sirven localmente; las licencias están junto a los recursos.

## Organización

| Ruta | Responsabilidad |
|---|---|
| Pages/Shared/_Layout.cshtml | Menú y recursos comunes |
| Pages/Proveedores/ | Módulo de Proveedores |
| Services/ProveedoresApiClient.cs | Cliente del Gateway, JSON, Bearer y errores |
| Services/DemoTokenProvider.cs | Token demo compartido por perfil en memoria |
| Services/DemoPerfilSession.cs | Perfil guardado en sesión |
| wwwroot/js/site.js | Validaciones tempranas, mensajes y protección de doble envío |
| wwwroot/js/proveedores-busqueda.mjs | Debounce, cancelación y actualización parcial del listado |

El buscador consulta nombre, teléfono y correo con debounce 300 ms. Limpiar cancela solicitudes pendientes, vuelve a la página 1 y conserva pestaña/orden. MongoDB aplica prioridad y orden antes de paginar. Solo se ofrecen Nombre A-Z y Nombre Z-A.

Crear/editar conservan valores al fallar y muestran errores por campo. Teléfono: 8 dígitos nacionales, inicio 4/6/7; no +591. Domain vuelve a validar todo. Razor mantiene la codificación de salida.

## Pruebas e integración

Desde la raíz:

```powershell
node scripts/probar-validaciones-frontend.cjs
node scripts/probar-busqueda-dinamica.mjs
dotnet test Servicio4.Proveedores/Proveedores.slnx
```

Las pruebas JavaScript usan el código real con un DOM mínimo o reloj controlado; no sustituyen pruebas visuales de todos los navegadores. Las de Razor usan TestServer.

Para nuevos módulos consultar la [Guía de integración](../../GUIA_INTEGRACION_GRUPO1.md). Crear páginas y cliente propios; reutilizar layout y recursos sin modificar Proveedores. Usuarios no está integrado en esta rama; las implementaciones de otras referencias se distinguen en INTEGRACION_GRUPO2.md de la raíz.

## Límites

La sesión y la caché están en memoria. El selector de perfiles no es login real, y Logout no revoca tokens compartidos. Keycloak real está pendiente en esta rama; Docker está disponible según la guía de la raíz. Los modales requieren JavaScript. Cada búsqueda recibe HTML de Razor y sustituye únicamente los nodos del listado; las búsquedas de subcadenas pueden recorrer documentos en MongoDB.
