using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Clientes.Adapters.In.Api.Security;
using PuntoMarket.Clientes.Adapters.Out.Persistence;
using PuntoMarket.Clientes.Application;
using PuntoMarket.Clientes.Application.Ports.Out;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwt = builder.Configuration.GetSection("Authentication:Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Authority = jwt["Authority"];
    options.Audience = jwt["Audience"];
    options.RequireHttpsMetadata = bool.TryParse(jwt["RequireHttpsMetadata"], out var https) && https;
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization(options =>
{
    var administrador = "Administrador";
    var cajero = "Cajero";
    options.AddPolicy(Policies.Lectura, p => p.RequireAuthenticatedUser().RequireAssertion(c =>
        c.User.Claims.Any(x => x.Type is "realm_access.roles" or "roles" && (x.Value.Contains(administrador) || x.Value.Contains(cajero)))));
    options.AddPolicy(Policies.Gestion, p => p.RequireAuthenticatedUser().RequireAssertion(c =>
        c.User.Claims.Any(x => x.Type is "realm_access.roles" or "roles" && (x.Value.Contains(administrador) || x.Value.Contains(cajero)))));
    options.AddPolicy(Policies.Baja, p => p.RequireAuthenticatedUser().RequireAssertion(c =>
        c.User.Claims.Any(x => x.Type is "realm_access.roles" or "roles" && x.Value.Contains(administrador))));
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/salud", () => Results.Ok(new { estado = "ok", servicio = "clientes" }));
app.Run();
