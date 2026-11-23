extern alias Comun;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
namespace Demo.Tests;

// Pipeline del frontend común: sesión real y antiforgery; Gateway simulado, sin persistencia.
public class ProductosComunesTests
{
    private sealed class Gateway:HttpMessageHandler
    {
        public readonly ConcurrentQueue<(string Metodo,string Ruta,string? Token,string Cuerpo)> Requests=new();
        public bool PrecioInvalido;
        public bool Inactivo;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req,CancellationToken ct) {
            var path=req.RequestUri!.AbsolutePath;
            var body=req.Content is null?"":await req.Content.ReadAsStringAsync(ct);
            Requests.Enqueue((req.Method.Method,path,req.Headers.Authorization?.Parameter,body));
            if(path=="/api/auth/login") {
                using var doc=JsonDocument.Parse(body); var rol=doc.RootElement.GetProperty("username").GetString();
                return Json(new{accessToken="jwt-sesion-productos",expiresIn=1800,username=rol,nombreCompleto="Prueba",rol,debeCambiarPassword=false});
            }
            if(path=="/api/productos/catalogos") return Json(new{categorias=new[]{new{id=1,nombre="Alimentos"}},empaques=new[]{new{id=1,nombre="Unidad"}}});
            if(path=="/api/proveedores") return Json(new{items=new[]{new{id="proveedor-texto",nombreEmpresa="Proveedor desde Gateway",estaActivo=true}},pagina=1,tamanoPagina=100,total=1,totalPaginas=1});
            if(PrecioInvalido && (req.Method==HttpMethod.Put || (req.Method==HttpMethod.Post && path=="/api/productos"))) return Json(new{detail="El precio de venta debe ser mayor o igual al costo.",code="Producto.PrecioVentaInvalido",errors=new{precioVenta=new[]{"Precio de venta inválido."}}},HttpStatusCode.BadRequest);
            var producto=new{id=1,codigo="SKU-1",nombre="Producto real de contrato",costo=10,precioVenta=15,categoriaId=1,empaqueId=1,proveedorId="proveedor-texto",estaActivo=!Inactivo,idUsuarioCreacion="sub-api",fechaCreacion=DateTime.UtcNow};
            if(req.Method==HttpMethod.Delete) return new(HttpStatusCode.NoContent);
            if(path=="/api/productos" && req.Method==HttpMethod.Get) return Json(new{items=new[]{producto},pagina=1,tamanoPagina=10,total=1,totalPaginas=1});
            return Json(producto);
        }
        private static HttpResponseMessage Json(object value,HttpStatusCode status=HttpStatusCode.OK)=>new(status){Content=JsonContent.Create(value)};
    }
    private static WebApplicationFactory<Comun::PuntoMarket.Frontend.Auth.AuthApiAdapter> Host(Gateway g)=>
        new WebApplicationFactory<Comun::PuntoMarket.Frontend.Auth.AuthApiAdapter>().WithWebHostBuilder(b=>{
            b.UseEnvironment("Development");
            b.ConfigureTestServices(s=>{
                s.ConfigureAll<HttpClientFactoryOptions>(o=>o.HttpMessageHandlerBuilderActions.Add(h=>h.PrimaryHandler=g));
                s.RemoveAll<IDataProtectionProvider>();s.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            });
        });
    private static string Csrf(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    private static async Task Login(HttpClient c,string rol) {
        var html=await c.GetStringAsync("/Auth/Login");
        var r=await c.PostAsync("/Auth/Login",new FormUrlEncodedContent(new Dictionary<string,string>{["Usuario"]=rol,["Password"]="Password-123!",["__RequestVerificationToken"]=Csrf(html)}));
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);
    }
    [Fact] public async Task SinSesionRedirigeYPostSinAntiforgerySeRechaza() {
        using var g=new Gateway();await using var host=Host(g);using var c=host.CreateClient(new(){AllowAutoRedirect=false});
        Assert.Equal("/Auth/Login",(await c.GetAsync("/Productos")).Headers.Location?.ToString());
        await Login(c,"Administrador");
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync("/Productos?handler=Desactivar",new FormUrlEncodedContent(new Dictionary<string,string>{["id"]="1"}))).StatusCode);
        Assert.DoesNotContain(g.Requests,x=>x.Metodo=="DELETE");
    }
    [Fact] public async Task CajeroConsultaYNoPuedeEscribir() {
        using var g=new Gateway();await using var host=Host(g);using var c=host.CreateClient(new(){AllowAutoRedirect=false});await Login(c,"Cajero");
        var html=await c.GetStringAsync("/Productos");
        Assert.Contains("Producto real de contrato",html);Assert.DoesNotContain("Nuevo producto",html);
        foreach(var handler in new[]{"Nuevo","Editar","Guardar","Desactivar"}) {
            var r=await c.PostAsync("/Productos?handler="+handler,new FormUrlEncodedContent(new Dictionary<string,string>{["id"]="1",["__RequestVerificationToken"]=Csrf(html)}));
            Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode);
        }
        Assert.DoesNotContain(g.Requests,x=>x.Ruta.StartsWith("/api/productos")&&x.Metodo!="GET");
    }
    [Fact] public async Task AdministradorCargaSelectoresYConservaErrorPrecio() {
        using var g=new Gateway();await using var host=Host(g);using var c=host.CreateClient(new(){AllowAutoRedirect=false});await Login(c,"Administrador");
        var html=await c.GetStringAsync("/Productos");
        var r=await c.PostAsync("/Productos?handler=Editar",new FormUrlEncodedContent(new Dictionary<string,string>{["id"]="1",["__RequestVerificationToken"]=Csrf(html)}));
        html=await r.Content.ReadAsStringAsync();
        Assert.Contains("Proveedor desde Gateway",html);Assert.Contains("Alimentos",html);
        g.PrecioInvalido=true;
        var form=new Dictionary<string,string>{["__RequestVerificationToken"]=Csrf(html),["EditarId"]="1",["Form.Codigo"]="SKU-1",["Form.Nombre"]="Conservar nombre",
            ["Form.Costo"]="10",["Form.PrecioVenta"]="9",["Form.CategoriaId"]="1",["Form.EmpaqueId"]="1",["Form.ProveedorId"]="proveedor-texto",["Form.MotivoCambioPrecio"]="Ajuste"};
        r=await c.PostAsync("/Productos?handler=Guardar",new FormUrlEncodedContent(form));
        html=WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK,r.StatusCode);Assert.Contains("El precio de venta",html);Assert.Contains("Conservar nombre",html);Assert.Contains("data-auto-open=\"true\"",html);
        var put=g.Requests.Last(x=>x.Metodo=="PUT");Assert.Equal("jwt-sesion-productos",put.Token);
        Assert.DoesNotContain("idUsuario",put.Cuerpo);
        g.PrecioInvalido=false;form["__RequestVerificationToken"]=Csrf(html);form["Form.PrecioVenta"]="16";
        Assert.Equal(HttpStatusCode.Redirect,(await c.PostAsync("/Productos?handler=Guardar",new FormUrlEncodedContent(form))).StatusCode);
        html=await c.GetStringAsync("/Productos");
        Assert.Equal(HttpStatusCode.Redirect,(await c.PostAsync("/Productos?handler=Desactivar",new FormUrlEncodedContent(new Dictionary<string,string>{["id"]="1",["__RequestVerificationToken"]=Csrf(html)}))).StatusCode);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Cajero")]
    public async Task DetalleMuestraCatalogosSinEscribirYNoHayReactivacion(string rol) {
        using var g=new Gateway();g.Inactivo=true;await using var host=Host(g);
        using var c=host.CreateClient(new(){AllowAutoRedirect=false});await Login(c,rol);
        var html=await c.GetStringAsync("/Productos?Estado=Inactivos");
        var tabla=Regex.Match(html,"<tbody>[\\s\\S]*?</tbody>").Value;
        Assert.Contains("bi-eye",tabla);Assert.DoesNotContain("bi-pencil-square",tabla);
        Assert.DoesNotContain("bi-trash",tabla);Assert.DoesNotContain("handler=Reactivar",html);
        var r=await c.PostAsync("/Productos?handler=Detalle",new FormUrlEncodedContent(new Dictionary<string,string>{["id"]="1",["__RequestVerificationToken"]=Csrf(html)}));
        html=WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        Assert.Contains("Detalles del producto",html);Assert.Contains("Alimentos",html);
        Assert.Contains("Unidad",html);Assert.Contains("Proveedor desde Gateway",html);
        Assert.Contains("Inactivo",html);
        Assert.DoesNotContain(g.Requests,x=>x.Ruta.StartsWith("/api/productos")&&x.Metodo!="GET");
    }
    [Fact]
    public async Task AltaAbreModalSinMotivoYActivosOfrecenTresIconos() {
        using var g=new Gateway();await using var host=Host(g);
        using var c=host.CreateClient(new(){AllowAutoRedirect=false});await Login(c,"Administrador");
        var html=await c.GetStringAsync("/Productos");
        foreach(var icono in new[]{"bi-eye","bi-pencil-square","bi-trash"}) Assert.Contains(icono,html);
        Assert.DoesNotContain("onsubmit=",html);
        var r=await c.PostAsync("/Productos?handler=Nuevo",new FormUrlEncodedContent(new Dictionary<string,string>{["__RequestVerificationToken"]=Csrf(html)}));
        html=await r.Content.ReadAsStringAsync();
        Assert.Contains("id=\"productoModal\"",html);Assert.Contains("data-auto-open=\"true\"",html);
        Assert.DoesNotContain("name=\"Form.MotivoCambioPrecio\"",html);
        Assert.DoesNotContain("validation-summary-errors",html);
        Assert.DoesNotContain("input-validation-error",html);
        Assert.DoesNotContain("field-validation-error",html);

        var crear=new Dictionary<string,string>{["__RequestVerificationToken"]=Csrf(html),["Form.Codigo"]="SKU-NUEVO",["Form.Nombre"]="Alta sin motivo",
            ["Form.Costo"]="10",["Form.PrecioVenta"]="15",["Form.CategoriaId"]="1",["Form.EmpaqueId"]="1",["Form.ProveedorId"]="proveedor-texto"};
        Assert.Equal(HttpStatusCode.Redirect,(await c.PostAsync("/Productos?handler=Guardar",new FormUrlEncodedContent(crear))).StatusCode);
        Assert.Contains(g.Requests,x=>x.Metodo=="POST"&&x.Ruta=="/api/productos");
    }

    [Fact]
    public async Task AltaVaciaValidaAlGuardarYReabrirReiniciaCamposYErrores() {
        using var g=new Gateway();await using var host=Host(g);
        using var c=host.CreateClient(new(){AllowAutoRedirect=false});await Login(c,"Administrador");
        var html=await c.GetStringAsync("/Productos");
        var r=await c.PostAsync("/Productos?handler=Guardar",new FormUrlEncodedContent(new Dictionary<string,string>{["__RequestVerificationToken"]=Csrf(html)}));
        html=await r.Content.ReadAsStringAsync();
        Assert.Contains("validation-summary-errors",html);
        Assert.Contains("input-validation-error",html);
        Assert.Contains("data-auto-open=\"true\"",html);
        Assert.DoesNotContain(g.Requests,x=>x.Metodo=="POST"&&x.Ruta=="/api/productos");
        // Opening creation must ignore any stale posted form/model-state values.
        r=await c.PostAsync("/Productos?handler=Nuevo&Estado=Inactivos&OrdenarPor=Costo",new FormUrlEncodedContent(new Dictionary<string,string>{
            ["__RequestVerificationToken"]=Csrf(html),["Form.Codigo"]="CODIGO-ANTERIOR",["Form.Nombre"]="Nombre anterior",["Form.Costo"]="37",
            ["Form.PrecioVenta"]="38",["Form.CategoriaId"]="1",["Form.EmpaqueId"]="1",["Form.ProveedorId"]="proveedor-texto",["EditarId"]="1"}));
        html=await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain("validation-summary-errors",html);Assert.DoesNotContain("input-validation-error",html);
        Assert.DoesNotContain("field-validation-error",html);Assert.DoesNotContain("CODIGO-ANTERIOR",html);Assert.DoesNotContain("Nombre anterior",html);
        foreach(var field in new[]{"Costo","PrecioVenta"}) {
            var input=Regex.Match(html,"<input[^>]*name=\"Form."+field+"\"[^>]*>").Value;
            Assert.Contains("value=\"\"",input);
        }
        Assert.Contains("Estado=Inactivos",html);
    }
    [Fact]
    public async Task ErrorApiEnAltaConservaDatosYReabrirDespuesDeCancelarQuedaLimpio() {
        using var g=new Gateway();g.PrecioInvalido=true;await using var host=Host(g);
        using var c=host.CreateClient(new(){AllowAutoRedirect=false});await Login(c,"Administrador");
        var html=await c.GetStringAsync("/Productos");
        var values=new Dictionary<string,string>{["__RequestVerificationToken"]=Csrf(html),["Form.Codigo"]="SKU-ERROR",["Form.Nombre"]="Conservar alta",
            ["Form.Costo"]="10",["Form.PrecioVenta"]="9",["Form.CategoriaId"]="1",["Form.EmpaqueId"]="1",["Form.ProveedorId"]="proveedor-texto"};
        var r=await c.PostAsync("/Productos?handler=Guardar",new FormUrlEncodedContent(values));
        html=WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK,r.StatusCode);Assert.Contains("data-auto-open=\"true\"",html);
        Assert.Contains("El precio de venta debe ser mayor o igual al costo.",html);Assert.Contains("Precio de venta inv\u00e1lido.",html);
        Assert.Contains("value=\"SKU-ERROR\"",html);Assert.Contains("value=\"Conservar alta\"",html);
        foreach(var field in new[]{"CategoriaId","EmpaqueId","ProveedorId"}) {
            var select=Regex.Match(html,"<select[^>]*name=\"Form."+field+"\"[^>]*>[\\s\\S]*?</select>").Value;
            Assert.Contains("selected=\"selected\"",select);
        }
        // The shared close/cancel handler navigates back to the listing GET.
        html=await c.GetStringAsync("/Productos");
        r=await c.PostAsync("/Productos?handler=Nuevo",new FormUrlEncodedContent(new Dictionary<string,string>{["__RequestVerificationToken"]=Csrf(html)}));
        html=await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SKU-ERROR",html);Assert.DoesNotContain("Conservar alta",html);
        Assert.DoesNotContain("validation-summary-errors",html);Assert.DoesNotContain("input-validation-error",html);
    }
}
