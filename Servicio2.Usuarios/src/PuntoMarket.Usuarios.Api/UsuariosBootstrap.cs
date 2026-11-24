using PuntoMarket.Usuarios.Application.DependencyInjection;
using PuntoMarket.Usuarios.Api.Documentation;
using PuntoMarket.Usuarios.Api.Security;
using PuntoMarket.Usuarios.Infrastructure.DependencyInjection;
namespace PuntoMarket.Usuarios.Api;
public static class UsuariosBootstrap
{
    public static WebApplication Crear(string[] args, Action<WebApplicationBuilder>? configurar = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ApplicationName = typeof(UsuariosBootstrap).Assembly.FullName });
        configurar?.Invoke(builder);
        var documentar = builder.Environment.IsDevelopment();
        builder.Services.AddControllers();
        builder.Services.AgregarGestionDeUsuarios();
        builder.Services.AgregarInfraestructuraGestion(builder.Configuration);
        builder.Services.AgregarAutenticacionKeycloak(builder.Configuration);
        builder.Services.AddAuthorization();
        if (documentar) builder.Services.AgregarDocumentacionUsuarios();
        var app = builder.Build();
        app.UseHttpsRedirection();
        if (documentar)
        {
            app.UseSwagger();
            app.UseSwaggerUI(o => { o.SwaggerEndpoint("v1/swagger.json", "PuntoMarket.Usuarios v1"); o.ConfigObject.PersistAuthorization = false; });
        }
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapGet("/api/salud", () => Results.Ok(new { estado = "activo" })).AllowAnonymous();
        return app;
    }
}
