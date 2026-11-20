using Frontend.Demo.Services;

// =====================================================
// DEMO TEMPORAL - FRONTEND RAZOR PAGES
// REEMPLAZAR EN FRONTEND DEFINITIVO
// INTEGRACIÓN GRUPO 2
//
// QUÉ HACE: UI PuntoMarket para demostrar el CRUD de Proveedores. Consume EXCLUSIVAMENTE el API Gateway
//   (ApiGateway:BaseUrl); nunca llama directamente al microservicio. No contiene lógica de negocio.
//
// QUÉ ES TEMPORAL Y DEBE SUSTITUIRSE cuando exista el frontend definitivo:
//   - Services/DemoPerfilSession.cs y Services/DemoTokenProvider.cs: el selector Administrador/Cajero y la
//     obtención del token de demostración. Reemplazar por el login real del emisor acordado y enviar
//     ese token en el encabezado Authorization.
//   El CRUD de Usuarios no es automáticamente el proveedor de identidad: acordarlo con el Grupo 2.
//   - Pages/Login, Pages/Logout y el menú de perfil de Pages/Shared/_Layout.cshtml.
// QUÉ SE PUEDE CONSERVAR: Services/ProveedoresApiClient.cs (cliente tipado del Gateway), los Models y las
//   páginas de Proveedores (solo cambiar de dónde sale el token).
// QUÉ NO DEBE MODIFICARSE: las rutas /api/proveedores... que se consumen a través del Gateway.
// CÓMO COMPROBAR: abrir /Login, elegir un perfil y comprobar las políticas de la API.
// =====================================================
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<DemoPerfilRequeridoFilter>();
builder.Services.AddRazorPages(o => o.Conventions.ConfigureFilter(
    new Microsoft.AspNetCore.Mvc.ServiceFilterAttribute(typeof(DemoPerfilRequeridoFilter))));
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(60);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// Cliente HTTP hacia el API GATEWAY (no hacia el microservicio).
// INTEGRACIÓN: Los clientes de nuevos módulos reutilizan este cliente nombrado y ApiGateway:BaseUrl.
//   Sus rutas/DTOs pertenecen a cada módulo; nunca incluir conexiones a sus bases en este frontend.
builder.Services.AddHttpClient("ApiGateway", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiGateway:BaseUrl"]
        ?? throw new InvalidOperationException("Falta la configuración ApiGateway:BaseUrl."));
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddSingleton<DemoTokenProvider>();
builder.Services.AddScoped<DemoPerfilSession>();
builder.Services.AddScoped<ProveedoresApiClient>();
// DEMO TEMPORAL: mismo Gateway y perfil; sustituir identidad por la sesión real del Grupo 2.
builder.Services.AddScoped<ClientesApiClient>();
// INTEGRACIÓN: Registrar junto a este cliente el cliente HTTP del nuevo módulo, una vez conocido
//   su contrato. Crear sus páginas en Pages/<Modulo>; menú y recursos comunes en Pages/Shared/_Layout.

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapGet("/health", () => Results.Ok("ok"));
app.MapRazorPages();

app.Run();

// Punto de entrada accesible para pruebas del frontend con WebApplicationFactory.
public partial class Program { }
