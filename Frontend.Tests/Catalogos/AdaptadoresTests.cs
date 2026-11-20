using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Frontend.Auth;
using PuntoMarket.Frontend.Catalogos;
using PuntoMarket.Frontend.Tests.Auth;

namespace PuntoMarket.Frontend.Tests.Catalogos;

public class AdaptadoresTests
{
    private sealed class Gateway : HttpMessageHandler
    {
        public HttpStatusCode Status = HttpStatusCode.OK;
        public string? Path, Token, Body, Method;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            Path=r.RequestUri!.PathAndQuery; Token=r.Headers.Authorization?.Parameter;
            Method=r.Method.Method; Body=r.Content is null ? null : await r.Content.ReadAsStringAsync(ct);
            return new(Status) { Content=JsonContent.Create((int)Status>=400
                ? (object)new { detail="Revise el campo.", code="Validacion.Prueba", errors=new { razonSocial=new[]{"Error de campo"} } }
                : r.Method==HttpMethod.Get && !Path.EndsWith("/2") && !Path.EndsWith("/"+new string('a',24))
                    ? new { items=Array.Empty<object>(), pagina=2, tamanoPagina=10, total=12, totalPaginas=2 }
                    : new { id=2, nitCi="12345", razonSocial="EMPRESA" }) };
        }
    }
    private static ServiceProvider Servicios(Gateway gateway)
    {
        var services=new ServiceCollection();
        var context=new DefaultHttpContext { Session=new FakeSession() };
        TokenProvider.Instance.Guardar(context.Session,"jwt-sesion-prueba");
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext=context });
        services.AddSingleton<ITokenProvider>(TokenProvider.Instance);
        services.AddTransient<BearerTokenHandler>();
        services.Configure<GatewayApiOptions>(o=>o.BaseUrl="http://gateway.test/");
        services.AddGatewayAdapter<IClientesApiClient,ClientesApiClient>().ConfigurePrimaryHttpMessageHandler(()=>gateway);
        services.AddGatewayAdapter<IProveedoresApiClient,ProveedoresApiClient>().ConfigurePrimaryHttpMessageHandler(()=>gateway);
        return services.BuildServiceProvider();
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task Listar_UsaGatewayBearerYPaginacion(bool proveedores)
    {
        using var gateway=new Gateway(); using var services=Servicios(gateway);
        if(proveedores)
        {
            var r=await services.GetRequiredService<IProveedoresApiClient>().BuscarAsync(new("A&B","Activos","Nombre",false,2,10),default);
            Assert.True(r.Ok); Assert.Equal(12,r.Data!.Total);
        }
        else
        {
            var r=await services.GetRequiredService<IClientesApiClient>().ListarAsync(new("A&B","Activos","RazonSocial",false,2,10),default);
            Assert.True(r.Ok); Assert.Equal(12,r.Data!.Total);
        }
        Assert.Equal("jwt-sesion-prueba",gateway.Token); Assert.Contains("busqueda=A%26B",gateway.Path);
        Assert.Contains("pagina=2",gateway.Path); Assert.Contains("tamanoPagina=10",gateway.Path);
    }
    [Theory]
    [InlineData(400)][InlineData(401)][InlineData(403)][InlineData(404)][InlineData(409)][InlineData(503)]
    public async Task Errores_ConservanEstadoCodigoYCampos_SinRenovarToken(int status)
    {
        using var gateway=new Gateway { Status=(HttpStatusCode)status }; using var services=Servicios(gateway);
        var c=await services.GetRequiredService<IClientesApiClient>().CrearAsync(new(){NitCi="12345",RazonSocial="empresa"},default);
        var p=await services.GetRequiredService<IProveedoresApiClient>().CrearAsync(new(){NombreEmpresa="Empresa",NumeroEmpresa="71234567"},default);
        Assert.False(c.Ok); Assert.False(p.Ok); Assert.Equal(status,c.Status); Assert.Equal(status,p.Status);
        if(status<500) {Assert.Equal("Validacion.Prueba",c.Codigo);Assert.Equal("Error de campo",c.ErroresCampos!["razonSocial"][0]);}
        else {Assert.Equal("Validacion.Prueba",c.Codigo);Assert.Null(c.ErroresCampos);Assert.DoesNotContain("Revise el campo",c.Mensaje);}
        Assert.Equal("jwt-sesion-prueba",gateway.Token);
    }
    [Fact]
    public async Task Clientes_MetodosCuerposYRutas_RespetanContrato()
    {
        using var gateway=new Gateway();using var services=Servicios(gateway);
        var api=services.GetRequiredService<IClientesApiClient>();var form=new ClienteFormVm {NitCi="12345",RazonSocial="  compañía   peña  "};
        await api.CrearAsync(form,default); Assert.Equal("POST",gateway.Method);Assert.Equal("/api/clientes",gateway.Path);
        using(var json=JsonDocument.Parse(gateway.Body!)) Assert.Equal("COMPAÑÍA PEÑA",json.RootElement.GetProperty("razonSocial").GetString());
        await api.ActualizarAsync(2,form,default);Assert.Equal("PUT",gateway.Method);Assert.Equal("/api/clientes/2",gateway.Path);
        gateway.Status=HttpStatusCode.NoContent;
        Assert.True((await api.DesactivarAsync(2,default)).Ok);Assert.Equal("DELETE",gateway.Method);
        await api.ReactivarAsync(2,default);Assert.Equal("PATCH",gateway.Method);Assert.Equal("/api/clientes/2/reactivar",gateway.Path);
    }
    [Fact]
    public async Task Proveedores_MetodosYRutas_ConservanObjectId()
    {
        using var gateway=new Gateway {Status=HttpStatusCode.NoContent};using var services=Servicios(gateway);
        var api=services.GetRequiredService<IProveedoresApiClient>();var id=new string('a',24);var form=new ProveedorFormVm {NombreEmpresa="Empresa",NumeroEmpresa="71234567"};
        await api.CrearAsync(form,default);Assert.Equal("POST",gateway.Method);Assert.Equal("/api/proveedores",gateway.Path);
        await api.ActualizarAsync(id,form,default);Assert.Equal("PUT",gateway.Method);Assert.Equal("/api/proveedores/"+id,gateway.Path);
        await api.ObtenerAsync(id,default);Assert.Equal("GET",gateway.Method);
        await api.DesactivarAsync(id,default);Assert.Equal("DELETE",gateway.Method);
        await api.ReactivarAsync(id,default);Assert.Equal("PATCH",gateway.Method);Assert.EndsWith("/reactivar",gateway.Path);
    }
}
