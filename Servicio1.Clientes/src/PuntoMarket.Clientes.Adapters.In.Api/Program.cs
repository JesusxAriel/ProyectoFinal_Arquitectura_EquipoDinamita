using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using PuntoMarket.Clientes.Adapters.In.Api.Http;
using PuntoMarket.Clientes.Adapters.In.Api.Security;
using PuntoMarket.Clientes.Adapters.Out.Persistence;
using PuntoMarket.Clientes.Application;

// =====================================================
// RAÍZ DE COMPOSICIÓN DEL MICROSERVICIO CLIENTES (arquitectura hexagonal)
//   Adaptador de entrada (esta API)  ->  puertos de entrada (casos de uso, Application)
//   casos de uso -> Domain + puertos de salida  <-  adaptador de salida (MySQL, Persistence)
// Domain y Application no conocen este proyecto ni MySQL.
// =====================================================
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();                                   // casos de uso
builder.Services.AddPersistence(builder.Configuration);              // MySqlDataSource (Singleton) + repositorio MySQL (Adapter)
builder.Services.AddClientesAuthentication(builder.Configuration);   // JWT de Keycloak + roles

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = _ =>
        new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = 400,
            Title = "Datos inválidos",
            Detail = "Revise los tipos y formatos de los campos y parámetros de la solicitud.",
            Extensions = { ["code"] = "Solicitud.Invalida" }
        }))
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Clientes API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Pegue el JWT (sin la palabra Bearer)."
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseExceptionHandler();

// Swagger solo en Development o con Swagger__Enabled=true.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Salud anónima: la usan el healthcheck de Docker y la ruta /api/clientes/salud del Gateway.
app.MapGet("/api/salud", () => Results.Ok(new { estado = "activo", servicio = "clientes" })).AllowAnonymous();

app.Run();

// Necesario para WebApplicationFactory<Program> en pruebas de integración.
public partial class Program { }
