using PuntoMarket.Gateway.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();
