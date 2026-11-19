var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/salud", () => Results.Ok(new { estado = "activo" }));

app.Run();
