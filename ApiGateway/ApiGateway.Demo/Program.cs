// =====================================================
// GATEWAY TEMPORAL
// INTEGRACIÓN GRUPO 2
// ESTADO: TEMPORAL (reemplazable por el Gateway definitivo del equipo)
//
// QUÉ HACE ESTE ARCHIVO:
//   Reverse proxy YARP. Las rutas y destinos NO están en código: se leen de la sección
//   "ReverseProxy" de ApiGateway.Demo/appsettings.json (o de variables de entorno en Docker).
//   YARP reenvía el encabezado "Authorization" con la configuración actual,
//   de modo que Proveedores.API valida el JWT por sí misma. El Gateway NO valida tokens
//   (evita autenticación duplicada) y NO contiene lógica de negocio.
//
// AGREGAR RUTAS DE OTROS MICROSERVICIOS:
//   Añadir una entrada en ReverseProxy:Routes + otra en ReverseProxy:Clusters (appsettings.json).
//   No hace falta tocar este archivo.
// INTEGRACIÓN: Routes.<id>.Match.Path define el prefijo HTTP público; ClusterId debe coincidir
//   con una clave de Clusters. Clusters.<id>.Destinations.<id>.Address indica la URL interna.
//   Usar identificadores propios y destinos configurables; no compartir un cluster entre APIs
//   distintas salvo que sean instancias equivalentes del mismo servicio.
// INTEGRACIÓN: Evitar prefijos solapados para destinos diferentes y rutas globales que capturen
//   Proveedores. Conservar sus rutas raíz y catch-all; transformar prefijos solo si lo exige el contrato.
// EJEMPLO ORIENTATIVO: Usuarios del Grupo 2 se registrará en esa configuración cuando entregue
//   sus rutas reales. No asumir nombres de endpoints, métodos, permisos ni puerto de Usuarios.
//   No añadir comentarios a los JSON; ver GUIA_INTEGRACION_GRUPO1.md para el procedimiento.
//
// QUÉ DEBE MODIFICAR EL GRUPO 2: solo appsettings.json (rutas/destinos) y, si lo desean, mover
//   la validación de JWT al Gateway (opcional; la API seguiría validando).
// INTEGRACIÓN KEYCLOAK: El proxy no necesita cambiar para reenviar un Bearer de otro emisor.
//   Configurar Authority/audiencia/roles en cada API y el login real del frontend; retirar la ruta
//   dev-auth solo cuando sus clientes ya no dependan de DevIdentity. Usuarios CRUD no implica emisor JWT.
// QUÉ NO DEBE MODIFICARSE: la ruta /api/proveedores/{**catch-all} debe seguir apuntando a Proveedores.API.
// CÓMO COMPROBAR: GET {Gateway}/api/proveedores con un Bearer válido => 200; sin token => 401 (viene de la API).
// =====================================================
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// INTEGRACIÓN: Este health comprueba el Gateway, no sus destinos ni sus bases de datos.
app.MapGet("/health", () => Results.Ok("ok"));
app.MapReverseProxy();

app.Run();

// DEMO TEMPORAL: punto de entrada público para TestServer, sin iniciar puertos ni Docker.
public partial class Program { }
