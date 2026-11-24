using System.Text.Json;
using Xunit;

namespace PuntoMarket.Gateway.Tests;

public class RutasAnonimasTests
{
    private static JsonElement Rutas()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "Gateway", "appsettings.json")))
            directorio = directorio.Parent;

        Assert.NotNull(directorio);
        using var documento = JsonDocument.Parse(File.ReadAllText(Path.Combine(directorio.FullName, "Gateway", "appsettings.json")));
        return documento.RootElement.GetProperty("ReverseProxy").GetProperty("Routes").Clone();
    }

    [Fact]
    public void La_activacion_es_una_ruta_anonima_solo_para_post_hacia_usuarios()
    {
        var ruta = Rutas().GetProperty("usuarios-activacion-route");

        Assert.Equal("anonymous", ruta.GetProperty("AuthorizationPolicy").GetString());
        Assert.Equal("usuarios-cluster", ruta.GetProperty("ClusterId").GetString());
        Assert.Equal("/api/activacion", ruta.GetProperty("Match").GetProperty("Path").GetString());
        Assert.Equal(["POST"], ruta.GetProperty("Match").GetProperty("Methods").EnumerateArray().Select(m => m.GetString()));
    }

    [Fact]
    public void Solo_la_salud_y_la_activacion_son_anonimas()
    {
        var anonimas = Rutas().EnumerateObject()
            .Where(r => r.Value.GetProperty("AuthorizationPolicy").GetString() == "anonymous")
            .Select(r => r.Value.GetProperty("Match").GetProperty("Path").GetString())
            .Order()
            .ToArray();

        Assert.Equal(
            ["/api/activacion", "/api/clientes/salud", "/api/productos/salud", "/api/proveedores/salud", "/api/usuarios/salud"],
            anonimas);
    }

    [Fact]
    public void La_gestion_de_usuarios_sigue_exigiendo_administrador()
    {
        var gestion = Rutas().GetProperty("usuarios-route");

        Assert.Equal("/api/usuarios/{**catch-all}", gestion.GetProperty("Match").GetProperty("Path").GetString());
        Assert.Equal("SoloAdministrador", gestion.GetProperty("AuthorizationPolicy").GetString());
    }

    [Fact]
    public void Toda_ruta_tiene_una_politica_de_autorizacion_explicita()
    {
        foreach (var ruta in Rutas().EnumerateObject())
            Assert.True(ruta.Value.TryGetProperty("AuthorizationPolicy", out _), ruta.Name);
    }
}
