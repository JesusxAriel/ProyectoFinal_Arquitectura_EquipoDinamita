using PuntoMarket.Frontend.Auth;
using PuntoMarket.Frontend.Catalogos;
using PuntoMarket.Frontend.Usuarios;

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
        builder.Services.AddGatewayAdapter<IClientesApiClient, ClientesApiClient>();
        builder.Services.AddGatewayAdapter<IProveedoresApiClient, ProveedoresApiClient>();
        builder.Services.AddGatewayAdapter<IUsuariosApiAdapter, UsuariosApiAdapter>();

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
