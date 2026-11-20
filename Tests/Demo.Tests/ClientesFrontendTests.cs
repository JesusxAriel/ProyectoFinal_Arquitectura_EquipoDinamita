extern alias Frontend;
using System.Net;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Frontend::Frontend.Demo.Models;

namespace Demo.Tests;

public class ClientesFrontendTests
{
    public sealed class GatewayFake : HttpMessageHandler, IHttpClientFactory
    {
        public ConcurrentQueue<(string Method, string Path, string Body, string? Token)> Peticiones { get; } = new();
        public bool RechazarCampos { get; set; }
        public bool ListadoVacio { get; set; }
        public bool SinOpcionales { get; set; }
        public bool UnauthorizedPrimero { get; set; }
        public int Tokens;
        public HttpClient CreateClient(string name) => new(this, false) { BaseAddress = new Uri("http://gateway.test/") };
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            var path = r.RequestUri!.AbsolutePath;
            var body = r.Content is null ? "" : await r.Content.ReadAsStringAsync(ct);
            Peticiones.Enqueue((r.Method.Method, r.RequestUri.PathAndQuery, body, r.Headers.Authorization?.Parameter));
            if (path == "/dev-auth/token")
            { Tokens++; return Json(new { access_token = "jwt-test-" + Tokens, expires_in = 600 }); }
            if (path.StartsWith("/api/proveedores")) return Json(new { items = Array.Empty<object>(), total = 0, pagina = 1, tamanoPagina = 10, totalPaginas = 0 });
            if (UnauthorizedPrimero) { UnauthorizedPrimero = false; return new(HttpStatusCode.Unauthorized); }
            if (r.Method == HttpMethod.Get && path == "/api/clientes")
            {
                var q = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(r.RequestUri.Query);
                var estado = q["estado"].ToString(); var activo = estado != "Inactivos";
                return Json(new { items = ListadoVacio ? Array.Empty<object>() : new[] { Cliente(1, activo), Cliente(2, activo) }, total = ListadoVacio ? 0 : 22, pagina = int.Parse(q["pagina"]!), tamanoPagina = int.Parse(q["tamanoPagina"]!), totalPaginas = 3 });
            }
            if (r.Method == HttpMethod.Get) return Json(Cliente(int.Parse(path.Split('/').Last()), true));
            if (RechazarCampos) return Json(new { detail = "El NIT/CI ya está ocupado.", errors = new { nitCi = new[] { "NIT/CI duplicado en la API." } } }, HttpStatusCode.Conflict);
            if (r.Method == HttpMethod.Delete) return new(HttpStatusCode.NoContent);
            return Json(Cliente(99, true), r.Method == HttpMethod.Post ? HttpStatusCode.Created : HttpStatusCode.OK);
        }
        private object Cliente(int id, bool activo) => new { id, nitCi = id == 1 ? "0" : "12345678", razonSocial = id == 1 ? "Sin Nombre" : "Comercial Pérez & Hijos", correo = SinOpcionales ? null : "ventas@empresa.test", telefono = SinOpcionales ? " " : "71234567", estaActivo = activo, esGenerico = id == 1, idUsuarioCreacion = "test", idUsuarioActualizacion = (string?)null, fechaCreacion = DateTime.UtcNow, fechaActualizacion = (DateTime?)null };
        private static HttpResponseMessage Json(object data, HttpStatusCode code = HttpStatusCode.OK) => new(code) { Content = JsonContent.Create(data) };
    }
    internal static WebApplicationFactory<Frontend::Program> Crear(GatewayFake g) => new WebApplicationFactory<Frontend::Program>().WithWebHostBuilder(b =>
    {
        b.UseEnvironment("Development"); b.ConfigureLogging(l => l.ClearProviders());
        b.ConfigureTestServices(s =>
        {
            s.RemoveAll<IHttpClientFactory>(); s.AddSingleton<IHttpClientFactory>(g);
            s.RemoveAll<IDataProtectionProvider>(); s.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        });
    });
    private static string Csrf(string html) => WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    internal static async Task Login(HttpClient c, string perfil)
    {
        var html = await c.GetStringAsync("/Login");
        var r = await c.PostAsync("/Login",new FormUrlEncodedContent(new Dictionary<string,string> { ["perfilElegido"]=perfil,["__RequestVerificationToken"]=Csrf(html) }));
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);
    }
    internal static Dictionary<string,string> Form(string html) => new() { ["__RequestVerificationToken"]=Csrf(html),["Form.NitCi"]="123456789",["Form.RazonSocial"]="Cliente de prueba",["Form.Correo"]="ventas+demo@empresa.test",["Form.Telefono"]="71234567" };

    [Fact] public async Task SinPerfil_RedirigeASelector()
    { using var g=new GatewayFake(); await using var f=Crear(g); using var c=f.CreateClient(new(){AllowAutoRedirect=false}); var r=await c.GetAsync("/Clientes"); Assert.Equal(HttpStatusCode.Redirect,r.StatusCode); Assert.Contains("/Login",r.Headers.Location!.ToString()); }
    [Theory][InlineData("Administrador",true)][InlineData("Cajero",false)]
    public async Task NavegacionYAcciones_RespetanPerfil_YProtegenGenerico(string perfil,bool baja)
    {
        using var g=new GatewayFake(); await using var f=Crear(g); using var c=f.CreateClient(new(){AllowAutoRedirect=false}); await Login(c,perfil);
        var html=WebUtility.HtmlDecode(await c.GetStringAsync("/Clientes"));
        Assert.Contains("href=\"/Clientes",html); Assert.Contains("href=\"/Proveedores",html); Assert.Contains("Cambiar perfil",html); Assert.DoesNotContain("Sin Nombre está protegido.",html);
        Assert.Contains("Añadir cliente",html); Assert.Contains("Editar",html); Assert.Contains("Genérico protegido",html);
        Assert.Equal(baja,html.Contains("id=\"estadoClienteModal\""));
        var row=Regex.Match(html,"<tr><td>0</td>(.*?)</tr>",RegexOptions.Singleline).Value;
        Assert.DoesNotContain("EditarId",row); Assert.DoesNotContain("data-cliente-accion",row);
        var prov=WebUtility.HtmlDecode(await c.GetStringAsync("/Proveedores"));
        Assert.Equal(baja,prov.Contains("id=\"proveedorModal\""));
    }
    [Theory][InlineData("Administrador")][InlineData("Cajero")]
    public async Task CrearYEditar_EnviaContratoRealConBearer(string perfil)
    {
        using var g=new GatewayFake(); await using var f=Crear(g); using var c=f.CreateClient(new(){AllowAutoRedirect=false}); await Login(c,perfil);
        var html=await c.GetStringAsync("/Clientes?Nuevo=true"); var r=await c.PostAsync("/Clientes?handler=Guardar",new FormUrlEncodedContent(Form(html))); Assert.True(r.StatusCode == HttpStatusCode.Redirect, string.Join(" | ",Regex.Matches(WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync()),"<(?:span|div)[^>]*(?:invalid-feedback|alert-danger|validation-summary-errors)[^>]*>(.*?)</(?:span|div)>").Select(m=>m.Value)));
        html=await c.GetStringAsync("/Clientes?EditarId=2"); r=await c.PostAsync("/Clientes?handler=Guardar&EditarId=2",new FormUrlEncodedContent(Form(html))); Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);
        var writes=g.Peticiones.Where(x=>x.Method is "POST" or "PUT" && x.Path.StartsWith("/api/clientes")).ToArray(); Assert.Equal(2,writes.Length);
        Assert.All(writes,x=> { Assert.StartsWith("jwt-test-",x.Token); using var d=JsonDocument.Parse(x.Body); Assert.Equal(4,d.RootElement.EnumerateObject().Count()); Assert.True(d.RootElement.TryGetProperty("nitCi",out _)); Assert.Equal("CLIENTE DE PRUEBA",d.RootElement.GetProperty("razonSocial").GetString()); });
    }
    [Fact] public async Task ErroresApi_ReabrenModalYConservanCampos()
    {
        using var g=new GatewayFake { RechazarCampos=true }; await using var f=Crear(g); using var c=f.CreateClient(new(){AllowAutoRedirect=false}); await Login(c,"Cajero");
        var html=await c.GetStringAsync("/Clientes?Nuevo=true"); var r=await c.PostAsync("/Clientes?handler=Guardar",new FormUrlEncodedContent(Form(html))); html=WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK,r.StatusCode); Assert.Contains("data-cliente-auto-open=\"true\"",html); Assert.Contains("NIT/CI duplicado en la API.",html); Assert.Contains("CLIENTE DE PRUEBA",html);
    }
    [Fact] public async Task DatosInvalidos_NoEnviaEscritura()
    {
        using var g=new GatewayFake(); await using var f=Crear(g); using var c=f.CreateClient(new(){AllowAutoRedirect=false}); await Login(c,"Administrador");
        var html=await c.GetStringAsync("/Clientes?Nuevo=true"); var form=Form(html); form["Form.NitCi"]="0";form["Form.Telefono"]="abc";
        var r=await c.PostAsync("/Clientes?handler=Guardar",new FormUrlEncodedContent(form)); Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        Assert.DoesNotContain(g.Peticiones,x=>x.Path.StartsWith("/api/clientes") && x.Method=="POST");
    }
    [Theory][InlineData("Desactivar")][InlineData("Reactivar")]
    public async Task Cajero_NoPuedeCambiarEstadoAunqueFuercePost(string handler)
    {
        using var g=new GatewayFake(); await using var f=Crear(g); using var c=f.CreateClient(new(){AllowAutoRedirect=false}); await Login(c,"Cajero");
        var html=await c.GetStringAsync("/Clientes"); var r=await c.PostAsync("/Clientes?handler="+handler,new FormUrlEncodedContent(new Dictionary<string,string>{["id"]="2",["__RequestVerificationToken"]=Csrf(html)}));
        Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode); Assert.DoesNotContain(g.Peticiones,x=>x.Method is "DELETE" or "PATCH");
    }
    [Theory][InlineData("Desactivar","DELETE")][InlineData("Reactivar","PATCH")]
    public async Task Administrador_CambiaEstadoPorGateway(string handler,string method)
    {
        using var g=new GatewayFake(); await using var f=Crear(g); using var c=f.CreateClient(new(){AllowAutoRedirect=false}); await Login(c,"Administrador");
        var html=await c.GetStringAsync("/Clientes"); var r=await c.PostAsync("/Clientes?handler="+handler,new FormUrlEncodedContent(new Dictionary<string,string>{["id"]="2",["__RequestVerificationToken"]=Csrf(html)}));
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode); Assert.Contains(g.Peticiones,x=>x.Method==method && x.Path.StartsWith("/api/clientes/2"));
    }
    [Theory][InlineData("Guardar")][InlineData("Desactivar")][InlineData("Reactivar")]
    public async Task GenericoProtegido_NoSeEnviaEscritura(string handler)
    {
        using var g=new GatewayFake(); await using var f=Crear(g); using var c=f.CreateClient(new(){AllowAutoRedirect=false}); await Login(c,"Administrador");
        var html=await c.GetStringAsync("/Clientes"); var form=Form(html);form["id"]="1";
        await c.PostAsync("/Clientes?handler="+handler+"&EditarId=1",new FormUrlEncodedContent(form));
        Assert.DoesNotContain(g.Peticiones,x=>x.Path.StartsWith("/api/clientes") && x.Method is "POST" or "PUT" or "DELETE" or "PATCH");
    }
    [Fact] public async Task Consulta_PropagaFiltrosOrdenYPagina_RenuevaSoloUn401()
    {
        using var g=new GatewayFake { UnauthorizedPrimero=true }; await using var f=Crear(g); using var c=f.CreateClient(new(){AllowAutoRedirect=false}); await Login(c,"Administrador");
        var html=WebUtility.HtmlDecode(await c.GetStringAsync("/Clientes?Busqueda=A%26B&Estado=Todos&OrdenarPor=NitCi&Descendente=true&Pagina=2&TamanoPagina=10&DetalleId=2"));
        Assert.Contains("Detalles del cliente",html); Assert.Contains("Comercial Pérez & Hijos",html); Assert.Equal(2,g.Tokens);
        Assert.Contains(g.Peticiones,x=>x.Path.Contains("busqueda=A%26B") && x.Path.Contains("ordenarPor=NitCi") && x.Path.Contains("pagina=2") && x.Token=="jwt-test-2");
    }
    [Theory][InlineData("NitCi","12345",true)][InlineData("NitCi","0",false)][InlineData("NitCi","abc12",false)][InlineData("RazonSocial","Comercial Pérez & Hijos",true)][InlineData("RazonSocial","<b>Empresa</b>",false)][InlineData("RazonSocial"," ",false)][InlineData("Telefono","",true)][InlineData("Telefono","44112233",true)][InlineData("Telefono","61234567",true)][InlineData("Telefono","71234567",true)][InlineData("Telefono","+59171234567",false)][InlineData("Correo","ventas+demo@empresa.com.bo",true)][InlineData("Correo","a@@b.com",false)]
    public void Formulario_AlineadoConContrato(string campo,string valor,bool valido)
    {
        var form=new ClienteFormVm { NitCi="12345",RazonSocial="Empresa",Correo=null,Telefono=null };
        typeof(ClienteFormVm).GetProperty(campo)!.SetValue(form,valor);
        Assert.Equal(valido,!form.Validate(new ValidationContext(form)).Any());
    }
}
