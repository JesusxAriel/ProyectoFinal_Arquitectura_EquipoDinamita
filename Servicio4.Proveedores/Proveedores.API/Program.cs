using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Proveedores.API.Http;
using Proveedores.API.Security;
using Proveedores.Application;
using Proveedores.Infrastructure;

// =====================================================
// RAÍZ DE COMPOSICIÓN DEL MICROSERVICIO PROVEEDORES (definitivo)
// Aquí se conectan las capas: Application (casos de uso) + Infrastructure (MongoDB, adaptadores)
// + seguridad JWT. Domain y Application no conocen este proyecto.
// =====================================================
var builder = WebApplication.CreateBuilder(args);

// INTEGRACIÓN: Recorrido normal: Controller -> caso de uso (puertos Application) -> Domain y
//   repositorio Adapter de Infrastructure. Registrar otro CRUD en el Gateway no modifica estas capas.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);       // MongoClient Singleton, repositorio Mongo, adaptadores
builder.Services.AddProveedoresAuthentication(builder.Configuration); // JWT + roles (INTEGRACIÓN GRUPO 2 - KEYCLOAK)

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = _ =>
        new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = 400, Title = "Datos inválidos", Detail = "Revise los tipos y formatos de los campos y parámetros de la solicitud.",
            Extensions = { ["code"] = "Solicitud.Invalida" }
        }))
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Proveedores API", Version = "v1" });
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

// Swagger habilitado en Development o con Swagger__Enabled=true (el docker-compose de desarrollo lo activa).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// Necesario para WebApplicationFactory<Program> en Proveedores.Tests.
public partial class Program { }
