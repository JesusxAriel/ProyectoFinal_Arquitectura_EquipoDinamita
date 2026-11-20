using PuntoMarket.Frontend.Auth;
using PuntoMarket.Frontend.Clientes;

namespace PuntoMarket.Frontend;

public static class FrontendBootstrap
{
    public static WebApplication Crear(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorPages();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSession();
        builder.Services.AddAutenticacionPorSesion(builder.Configuration);
        builder.Services.AddGatewayAdapter<IClientesApiAdapter, ClientesApiAdapter>();

        var app = builder.Build();
        app.UseExceptionHandler("/Error");
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.UseAutenticacionPorSesion();
        app.MapRazorPages();
        return app;
    }
}
