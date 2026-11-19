namespace PuntoMarket.Frontend;

public static class FrontendBootstrap
{
    public static WebApplication Crear(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorPages();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSession();

        var app = builder.Build();
        app.UseExceptionHandler("/Error");
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.MapRazorPages();
        return app;
    }
}
