using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Proveedores.Application.Dtos;

namespace Proveedores.Tests;

public class SolicitudValidacionesTests(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private HttpClient Admin() => fabrica.ClienteCon(ApiFactory.Token("validaciones-admin", "Administrador"));

    [Theory]
    [InlineData("nombreEmpresa", "<script>alert(1)</script>")]
    [InlineData("numeroEmpresa", "71234567\n")]
    [InlineData("correoReferencia", "ventas@empresa..com")]
    public async Task CampoComercialInvalido_400ConErrorEspecifico_SinPersistencia(string campo, string valor)
    {
        using var cliente = Admin();
        var cuerpo = new Dictionary<string, object> { ["nombreEmpresa"] = "No persistir", ["numeroEmpresa"] = "71234567",
            ["correoReferencia"] = "ventas@empresa.com", ["esAutogestionado"] = false };
        cuerpo[campo] = valor;
        var antes = fabrica.Repositorio.Todos.Count;
        using var r = await cliente.PostAsJsonAsync("/api/proveedores", cuerpo);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty(campo, out _));
        Assert.Equal(antes, fabrica.Repositorio.Todos.Count);
    }

    [Theory]
    [InlineData("?pagina=abc")]
    [InlineData("?pagina=9999999999999999")]
    [InlineData("?estado=900")]
    [InlineData("?ordenarPor=900")]
    [InlineData("?busqueda=hola%0Amundo")]
    [InlineData("?busqueda=%E2%80%8B")]
    public async Task ParametroInvalido_400Controlado(string query)
    {
        using var cliente = Admin();
        using var r = await cliente.GetAsync("/api/proveedores" + query);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var texto = await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Exception", texto);
        Assert.DoesNotContain("System.", texto);
    }

    [Fact]
    public async Task BusquedaLarga_400_BusquedaBlanca_200()
    {
        using var cliente = Admin();
        using var larga = await cliente.GetAsync("/api/proveedores?busqueda=" + new string('a',151));
        using var blanca = await cliente.GetAsync("/api/proveedores?busqueda=%20%20");
        Assert.Equal(HttpStatusCode.BadRequest, larga.StatusCode);
        Assert.Equal(HttpStatusCode.OK, blanca.StatusCode);
    }

    [Fact]
    public async Task ObjetoComoNombre_NoAceptaOperadoresMongo()
    {
        using var cliente = Admin();
        using var r = await cliente.PostAsync("/api/proveedores", new StringContent(
            """{"nombreEmpresa":{"$ne":null},"numeroEmpresa":"71234567","esAutogestionado":false}""", Encoding.UTF8,"application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("Solicitud.Invalida", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task IdMalformado_Conserva404EnLecturaYEscrituras()
    {
        using var cliente = Admin();
        const string ruta = "/api/proveedores/no-es-objectid";
        using var get = await cliente.GetAsync(ruta);
        using var put = await cliente.PutAsJsonAsync(ruta, new GuardarProveedorRequest("Valido", "71234567", null, false));
        using var delete = await cliente.DeleteAsync(ruta);
        using var patch = await cliente.PatchAsync(ruta + "/reactivar", null);
        foreach (var r in new[] { get, put, delete, patch }) Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task BajaImpideEdicion_YReactivacionRespetaNombreNormalizado()
    {
        using var cliente = Admin();
        var nombre = "Café Validaciones " + Guid.NewGuid().ToString("N");
        var cuerpo = new GuardarProveedorRequest(nombre, "61234567", null, false);
        using var creado = await cliente.PostAsJsonAsync("/api/proveedores", cuerpo);
        var dto = (await creado.Content.ReadFromJsonAsync<ProveedorDto>())!;
        using var baja = await cliente.DeleteAsync("/api/proveedores/" + dto.Id);
        using var edicion = await cliente.PutAsJsonAsync("/api/proveedores/" + dto.Id, cuerpo);
        Assert.Equal(HttpStatusCode.Conflict, edicion.StatusCode);
        Assert.Contains("Proveedor.EdicionDeInactivo", await edicion.Content.ReadAsStringAsync());
        using var otro = await cliente.PostAsJsonAsync("/api/proveedores", cuerpo with { NombreEmpresa = nombre.Replace("Café", "CAFE") });
        Assert.Equal(HttpStatusCode.Created, otro.StatusCode);
        using var reactivar = await cliente.PatchAsync("/api/proveedores/" + dto.Id + "/reactivar", null);
        Assert.Equal(HttpStatusCode.Conflict, reactivar.StatusCode);
        Assert.Contains("Proveedor.ReactivacionConflictiva", await reactivar.Content.ReadAsStringAsync());
    }
}
