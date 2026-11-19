using System.Net;
using System.Net.Http.Json;
using Proveedores.Application.Dtos;

namespace Proveedores.Tests;

/// <summary>Pruebas de autenticación/autorización sobre la API real (pipeline completo, sin MongoDB).</summary>
public class AuthorizationTests(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private static GuardarProveedorRequest Cuerpo(string nombre) =>
        new(nombre, "70012345", "contacto@example.invalid", false);

    private HttpClient Admin() => fabrica.ClienteCon(ApiFactory.Token("sub-admin-001", "Administrador"));
    private HttpClient Cajero() => fabrica.ClienteCon(ApiFactory.Token("sub-cajero-001", "Cajero"));

    [Fact]
    public async Task SinToken_Listar_401()
    {
        var r = await fabrica.ClienteCon(null).GetAsync("/api/proveedores");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task SinToken_Crear_401()
    {
        var r = await fabrica.ClienteCon(null).PostAsJsonAsync("/api/proveedores", Cuerpo("Sin Token SA"));
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task TokenInvalido_401()
    {
        var r = await fabrica.ClienteCon("esto.no.es-un-jwt").GetAsync("/api/proveedores");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task TokenExpirado_401()
    {
        var expirado = ApiFactory.Token("sub-x", "Administrador", TimeSpan.FromMinutes(-10));
        var r = await fabrica.ClienteCon(expirado).GetAsync("/api/proveedores");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task TokenConAudienceIncorrecta_401()
    {
        var token = ApiFactory.Token("sub-x", "Administrador", audience: "otra-api");
        var r = await fabrica.ClienteCon(token).GetAsync("/api/proveedores");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Cajero_PuedeListar()
    {
        var r = await Cajero().GetAsync("/api/proveedores");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Cajero_NoPuedeCrear_403()
    {
        var r = await Cajero().PostAsJsonAsync("/api/proveedores", Cuerpo("Cajero Intenta SA"));
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Cajero_NoPuedeDesactivar_403()
    {
        var creado = await (await Admin().PostAsJsonAsync("/api/proveedores", Cuerpo("Para Desactivar SA")))
            .Content.ReadFromJsonAsync<ProveedorDto>();

        var r = await Cajero().DeleteAsync($"/api/proveedores/{creado!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Cajero_NoPuedeModificarNiReactivar_403()
    {
        var put = await Cajero().PutAsJsonAsync("/api/proveedores/000000000000000000000001", Cuerpo("X"));
        var patch = await Cajero().PatchAsync("/api/proveedores/000000000000000000000001/reactivar", null);
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
    }

    [Fact]
    public async Task TokenSinRolConocido_403()
    {
        var r = await fabrica.ClienteCon(ApiFactory.Token("sub-x", "otro-rol")).GetAsync("/api/proveedores");
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Admin_Crea_201_ConAuditoriaDelClaimSub()
    {
        var r = await Admin().PostAsJsonAsync("/api/proveedores", Cuerpo("Admin Crea SA"));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<ProveedorDto>();
        Assert.Equal("sub-admin-001", dto!.IdUsuarioAdmin);
        Assert.NotNull(r.Headers.Location);
    }

    [Fact]
    public async Task Admin_FlujoCompleto_Duplicado409_Desactivar204_Reactivar200()
    {
        var admin = Admin();
        var creado = await (await admin.PostAsJsonAsync("/api/proveedores", Cuerpo("Flujo Completo SA")))
            .Content.ReadFromJsonAsync<ProveedorDto>();

        var duplicado = await admin.PostAsJsonAsync("/api/proveedores", Cuerpo("  FLUJO   completo sa "));
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        var invalido = await admin.PostAsJsonAsync("/api/proveedores", Cuerpo("   "));
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);

        var actualizado = await admin.PutAsJsonAsync($"/api/proveedores/{creado!.Id}", Cuerpo("Flujo Completo Editado SA"));
        Assert.Equal(HttpStatusCode.OK, actualizado.StatusCode);

        var desactivado = await admin.DeleteAsync($"/api/proveedores/{creado.Id}");
        Assert.Equal(HttpStatusCode.NoContent, desactivado.StatusCode);

        var dobleDesactivar = await admin.DeleteAsync($"/api/proveedores/{creado.Id}");
        Assert.Equal(HttpStatusCode.Conflict, dobleDesactivar.StatusCode);

        var reactivado = await admin.PatchAsync($"/api/proveedores/{creado.Id}/reactivar", null);
        Assert.Equal(HttpStatusCode.OK, reactivado.StatusCode);
        Assert.True((await reactivado.Content.ReadFromJsonAsync<ProveedorDto>())!.EstaActivo);
    }

    [Fact]
    public async Task Admin_ProveedorInexistente_404()
    {
        var r = await Admin().GetAsync("/api/proveedores/000000000000000000000999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Health_EsAnonimo()
    {
        var r = await fabrica.ClienteCon(null).GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }
}
