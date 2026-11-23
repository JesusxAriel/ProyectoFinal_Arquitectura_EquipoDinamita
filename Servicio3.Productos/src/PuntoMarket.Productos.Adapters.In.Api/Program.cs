using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PuntoMarket.Productos.Application.Ports.In;
using PuntoMarket.Productos.Application.Ports.Out;
using PuntoMarket.Productos.Application.UseCases;
using PuntoMarket.Productos.Adapters.Out.Persistence;
using PuntoMarket.Productos.Adapters.In.Api.Integrations;
using PuntoMarket.Productos.Adapters.In.Api.Security;
using PuntoMarket.Productos.Adapters.In.Api.Http;

var builder = WebApplication.CreateBuilder(args);
// Singleton: una fuente/pool por proceso; conexiones de vida corta en cada operación.
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("PostgresConnection")
    ?? throw new InvalidOperationException("Configure ConnectionStrings__PostgresConnection.")));
builder.Services.AddSingleton<ProductosDatabaseInitializer>();
builder.Services.AddScoped<ProductoRepositoryCreator, PostgresProductoRepositoryCreator>();
builder.Services.AddScoped<IProductoRepository>(sp => sp.GetRequiredService<ProductoRepositoryCreator>().Crear());
builder.Services.AddScoped<IListarProductos,ListarProductos>();
builder.Services.AddScoped<IObtenerProducto,ObtenerProducto>();
builder.Services.AddScoped<IConsultarCatalogos,ConsultarCatalogos>();
builder.Services.AddScoped<ICrearProducto,CrearProducto>();
builder.Services.AddScoped<IActualizarProducto,ActualizarProducto>();
builder.Services.AddScoped<IDesactivarProducto,DesactivarProducto>();
builder.Services.AddProductosAuthentication(builder.Configuration);
builder.Services.AddHttpClient<IProveedorApiClient,ProveedorApiAdapter>(http => {
    http.BaseAddress=new Uri(builder.Configuration["ProveedoresApi:BaseUrl"] ?? "http://gateway:8080/");
    http.Timeout=TimeSpan.FromSeconds(10);
});
builder.Services.AddControllers().ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = _ =>
    new BadRequestObjectResult(new ProblemDetails {Status=400,Title="Datos inválidos",Detail="Revise los tipos y formatos de la solicitud.",Extensions={["code"]="Solicitud.Invalida"}}));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
var app=builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/salud",()=>Results.Ok(new {estado="activo",servicio="productos"})).AllowAnonymous();
if(app.Configuration.GetValue<bool>("Productos:InitializeOnStartup"))
    await app.Services.GetRequiredService<ProductosDatabaseInitializer>().InicializarAsync(app.Configuration.GetValue<bool>("Productos:SeedEnabled"));
app.Run();
public partial class Program { }
