using System.Text.Json;

namespace PuntoMarket.Gateway.Tests;

public sealed class RutasUsuariosTests
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

    [Theory]
    [InlineData("/api/usuarios/perfil")]
    [InlineData("/api/usuarios/perfil/password")]
    [InlineData("/api/usuarios/perfil/password/verificacion")]
    public void Las_rutas_del_perfil_exigen_token_valido_y_van_antes_que_la_de_administrador(string ruta)
    {
        var coincidencias = Rutas().EnumerateObject()
            .Where(r => r.Value.GetProperty("Match").GetProperty("Path").GetString() == ruta)
            .ToList();

        var definicion = Assert.Single(coincidencias).Value;
        Assert.Equal("TokenValido", definicion.GetProperty("AuthorizationPolicy").GetString());
        Assert.Equal("usuarios-cluster", definicion.GetProperty("ClusterId").GetString());
        Assert.True(definicion.GetProperty("Order").GetInt32() < 0);
    }

    [Fact]
    public void La_verificacion_de_contrasena_no_es_anonima()
    {
        var verificacion = Rutas().EnumerateObject()
            .Single(r => r.Value.GetProperty("Match").GetProperty("Path").GetString() == "/api/usuarios/perfil/password/verificacion");

        Assert.NotEqual("anonymous", verificacion.Value.GetProperty("AuthorizationPolicy").GetString());
    }
}
