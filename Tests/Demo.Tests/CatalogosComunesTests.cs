extern alias Comun;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Demo.Tests;

// Pruebas del pipeline Razor real (sesión + antiforgery + BearerTokenHandler). No abren bases ni puertos.
public class CatalogosComunesTests
{
    private const string IdProveedor="aaaaaaaaaaaaaaaaaaaaaaaa";
    private sealed class Reloj : TimeProvider { public DateTimeOffset Ahora=DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow()=>Ahora; }
    private sealed class Gateway : HttpMessageHandler
    {
        public readonly ConcurrentQueue<(string Method,string Path,string? Token,string Body)> Requests=new();
        public bool ErrorCampos;
        public int? ErrorStatus;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct)
        {
            var path=r.RequestUri!.AbsolutePath;var body=r.Content is null?"":await r.Content.ReadAsStringAsync(ct);
            Requests.Enqueue((r.Method.Method,r.RequestUri.PathAndQuery,r.Headers.Authorization?.Parameter,body));
            if(path=="/api/auth/login")
            {
                using var json=JsonDocument.Parse(body);var rol=json.RootElement.GetProperty("username").GetString()!;
                return Json(new {accessToken="jwt-comun-privado",expiresIn=1800,username=rol,nombreCompleto="Usuario prueba",rol,debeCambiarPassword=false});
            }
            if(ErrorStatus is {} status) return Json(new {detail="Error controlado"},(HttpStatusCode)status);
            if(ErrorCampos && r.Method is not null && r.Method!=HttpMethod.Get)
                return Json(new {detail="Revise los campos.",code="Prueba.Invalida",errors=new {razonSocial=new[]{"Razón social rechazada."},nombreEmpresa=new[]{"Nombre rechazado."}}},HttpStatusCode.BadRequest);
            var provider=path.StartsWith("/api/proveedores");
            object item=provider
                ? new {id=IdProveedor,nombreEmpresa="Empresa común",numeroEmpresa="71234567",correoReferencia=(string?)null,esAutogestionado=true,estaActivo=true,idUsuarioAdmin="uuid-prueba",idUsuarioActualizacion=(string?)null,fechaCreacion=DateTime.UtcNow,fechaActualizacion=(DateTime?)null}
                : new {id=2,nitCi="12345",razonSocial="CLIENTE COMÚN",correo=(string?)null,telefono=(string?)null,estaActivo=true,esGenerico=false,idUsuarioCreacion="uuid-prueba",idUsuarioActualizacion=(string?)null,fechaCreacion=DateTime.UtcNow,fechaActualizacion=(DateTime?)null};
            if(r.Method==HttpMethod.Get && path.Count(c=>c=='/')==2)
            {
                var q=Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(r.RequestUri.Query);
                return Json(new {items=new[]{item},total=22,pagina=int.Parse(q["pagina"]!),tamanoPagina=10,totalPaginas=3});
            }
            if(r.Method==HttpMethod.Delete)return new(HttpStatusCode.NoContent);
            return Json(item,r.Method==HttpMethod.Post?HttpStatusCode.Created:HttpStatusCode.OK);
        }
        private static HttpResponseMessage Json(object value,HttpStatusCode status=HttpStatusCode.OK)=>new(status){Content=JsonContent.Create(value)};
    }
    private static WebApplicationFactory<Comun::PuntoMarket.Frontend.Auth.AuthApiAdapter> Crear(Gateway gateway,Reloj? reloj=null)=>
        new WebApplicationFactory<Comun::PuntoMarket.Frontend.Auth.AuthApiAdapter>().WithWebHostBuilder(b=>{
            b.UseEnvironment("Development");b.ConfigureLogging(l=>l.ClearProviders());
            b.ConfigureTestServices(s=>{
                s.ConfigureAll<HttpClientFactoryOptions>(o=>o.HttpMessageHandlerBuilderActions.Add(builder=>builder.PrimaryHandler=gateway));
                s.RemoveAll<IDataProtectionProvider>();s.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                if(reloj is not null){s.RemoveAll<TimeProvider>();s.AddSingleton<TimeProvider>(reloj);}
            });
        });
    private static string Csrf(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    private static async Task Login(HttpClient c,string rol)
    {
        var html=await c.GetStringAsync("/Auth/Login");
        var r=await c.PostAsync("/Auth/Login",new FormUrlEncodedContent(new Dictionary<string,string>{["Usuario"]=rol,["Password"]="Password-123!",["__RequestVerificationToken"]=Csrf(html)}));
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);
    }
    private static Dictionary<string,string> Form(string html,bool proveedor)=>proveedor
        ? new(){["__RequestVerificationToken"]=Csrf(html),["Form.NombreEmpresa"]="Empresa prueba",["Form.NumeroEmpresa"]="71234567",["Form.CorreoReferencia"]="",["Form.EsAutogestionado"]="true"}
        : new(){["__RequestVerificationToken"]=Csrf(html),["Form.NitCi"]="12345678",["Form.RazonSocial"]="  compañía   peña  ",["Form.Correo"]="",["Form.Telefono"]=""};

    [Theory][InlineData("Clientes")][InlineData("Proveedores")]
    public async Task SinSesionYExpirada_RedirigenAlLogin(string modulo)
    {
        using var g=new Gateway();var reloj=new Reloj();await using var f=Crear(g,reloj);using var c=f.CreateClient(new(){AllowAutoRedirect=false});
        var r=await c.GetAsync("/"+modulo);Assert.Equal("/Auth/Login",r.Headers.Location!.ToString());
        await Login(c,"Administrador");reloj.Ahora=reloj.Ahora.AddHours(1);
        r=await c.GetAsync("/"+modulo);Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);Assert.Equal("/Auth/Login",r.Headers.Location!.ToString());
        Assert.DoesNotContain(g.Requests,x=>x.Path.StartsWith("/api/clientes")||x.Path.StartsWith("/api/proveedores"));
    }
    [Theory][InlineData("Clientes","Administrador")][InlineData("Clientes","Cajero")][InlineData("Proveedores","Administrador")][InlineData("Proveedores","Cajero")]
    public async Task ListadoYDetalle_ConSesionBearerFiltrosYSinIdsEnUrl(string modulo,string rol)
    {
        var provider=modulo=="Proveedores";using var g=new Gateway();await using var f=Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await Login(c,rol);
        var html=WebUtility.HtmlDecode(await c.GetStringAsync("/"+modulo+"?Busqueda=A%26B&Pagina=2&Estado=Inactivos"));
        Assert.Contains(provider?"Empresa común":"CLIENTE COMÚN",html);Assert.DoesNotContain("jwt-comun-privado",html);
        Assert.Contains(g.Requests,x=>x.Path.Contains("busqueda=A%26B")&&x.Path.Contains("pagina=2")&&x.Path.Contains("estado=Inactivos")&&x.Token=="jwt-comun-privado");
        foreach(Match match in Regex.Matches(html,"(?:href|action)=\"([^\"]*)\""))
        {Assert.DoesNotContain("EditarId",match.Value);Assert.DoesNotContain("DetalleId",match.Value);Assert.DoesNotContain(IdProveedor,match.Value);}
        var r=await c.PostAsync("/"+modulo+"?handler=Detalle",new FormUrlEncodedContent(new Dictionary<string,string>{["id"]=provider?IdProveedor:"2",["__RequestVerificationToken"]=Csrf(html)}));
        html=WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        Assert.Contains(provider?"Detalles del proveedor":"Detalles del cliente",html);Assert.Contains("No registrado",html);
    }
    [Theory][InlineData(false,"Administrador")][InlineData(false,"Cajero")][InlineData(true,"Administrador")]
    public async Task CrearEditarYEstado_ConservanContratosYPermisos(bool provider,string rol)
    {
        var modulo=provider?"Proveedores":"Clientes";using var g=new Gateway();await using var f=Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await Login(c,rol);
        var html=await c.GetStringAsync("/"+modulo+"?Nuevo=true");
        var r=await c.PostAsync("/"+modulo+"?handler="+(provider?"Crear":"Guardar"),new FormUrlEncodedContent(Form(html,provider)));
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);
        if(!provider){using var json=JsonDocument.Parse(g.Requests.Last(x=>x.Method=="POST"&&x.Path=="/api/clientes").Body);Assert.Equal("COMPAÑÍA PEÑA",json.RootElement.GetProperty("razonSocial").GetString());}
        html=await c.GetStringAsync("/"+modulo);
        var id=provider?IdProveedor:"2";
        r=await c.PostAsync("/"+modulo+"?handler=AbrirEditar",new FormUrlEncodedContent(new Dictionary<string,string>{["id"]=id,["__RequestVerificationToken"]=Csrf(html)}));
        html=await r.Content.ReadAsStringAsync();Assert.Equal(HttpStatusCode.OK,r.StatusCode);Assert.Contains("Editar",html);
        var form=Form(html,provider);form["EditarId"]=id;
        r=await c.PostAsync("/"+modulo+"?handler="+(provider?"Editar":"Guardar"),new FormUrlEncodedContent(form));Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);
        foreach(var handler in new[]{"Desactivar","Reactivar"})
        {
            html=await c.GetStringAsync("/"+modulo);
            r=await c.PostAsync("/"+modulo+"?handler="+handler,new FormUrlEncodedContent(new Dictionary<string,string>{["id"]=id,["__RequestVerificationToken"]=Csrf(html)}));
            Assert.Equal(rol=="Administrador"?HttpStatusCode.Redirect:HttpStatusCode.Forbidden,r.StatusCode);
        }
        Assert.Contains(g.Requests,x=>x.Method=="PUT");
        if(rol=="Administrador"){Assert.Contains(g.Requests,x=>x.Method=="DELETE");Assert.Contains(g.Requests,x=>x.Method=="PATCH");}
    }
    [Theory][InlineData("Crear")][InlineData("Editar")][InlineData("AbrirEditar")][InlineData("Desactivar")][InlineData("Reactivar")]
    public async Task Cajero_NoPuedeForzarEscrituraProveedor(string handler)
    {
        using var g=new Gateway();await using var f=Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await Login(c,"Cajero");var html=await c.GetStringAsync("/Proveedores");
        var form=Form(html,true);form["id"]=IdProveedor;form["EditarId"]=IdProveedor;
        var r=await c.PostAsync("/Proveedores?handler="+handler,new FormUrlEncodedContent(form));Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode);
        Assert.DoesNotContain(g.Requests,x=>x.Path.StartsWith("/api/proveedores")&&x.Method!="GET");
    }
    [Theory][InlineData("Clientes")][InlineData("Proveedores")]
    public async Task SinAntiforgery_NoEnviaEscrituras(string modulo)
    {
        using var g=new Gateway();await using var f=Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await Login(c,"Administrador");
        var r=await c.PostAsync("/"+modulo+"?handler="+(modulo=="Clientes"?"Guardar":"Crear"),new FormUrlEncodedContent(new Dictionary<string,string>()));Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);
        Assert.DoesNotContain(g.Requests,x=>x.Method=="POST"&&x.Path.StartsWith("/api/"+modulo.ToLowerInvariant()));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task ErrorPorCampo_ReabreFormularioYConservaValores(bool provider)
    {
        using var g=new Gateway();await using var f=Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await Login(c,"Administrador");var modulo=provider?"Proveedores":"Clientes";
        var html=await c.GetStringAsync("/"+modulo+"?Nuevo=true");g.ErrorCampos=true;
        var r=await c.PostAsync("/"+modulo+"?handler="+(provider?"Crear":"Guardar"),new FormUrlEncodedContent(Form(html,provider)));
        html=WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());Assert.Contains(provider?"Nombre rechazado.":"Razón social rechazada.",html);Assert.Contains(provider?"Empresa prueba":"COMPAÑÍA PEÑA",html);
        Assert.Contains(provider?"data-auto-open=\"true\"":"data-cliente-auto-open=\"true\"",html);
    }
    [Theory][InlineData(401)][InlineData(403)]
    public async Task RechazoApi_NoEmiteTokenAlternativo(int status)
    {
        using var g=new Gateway();await using var f=Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await Login(c,"Administrador");g.ErrorStatus=status;
        var r=await c.GetAsync("/Clientes");Assert.Equal(status,(int)r.StatusCode);Assert.DoesNotContain(g.Requests,x=>x.Path.Contains("dev-auth"));
    }
    [Theory][InlineData("Clientes")][InlineData("Proveedores")]
    public async Task RolDesconocido_NoAccedeAlModulo(string modulo)
    {
        using var g=new Gateway();await using var f=Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await Login(c,"Invitado");
        var r=await c.GetAsync("/"+modulo);Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode);
        Assert.DoesNotContain(g.Requests,x=>x.Path.StartsWith("/api/clientes")||x.Path.StartsWith("/api/proveedores"));
    }
    [Theory][InlineData("Clientes","abc")][InlineData("Clientes","-1")][InlineData("Proveedores","abc")]
    public async Task Detalles_RechazaIdsInvalidosSinConsultarApi(string modulo,string id)
    {
        using var g=new Gateway();await using var f=Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await Login(c,"Administrador");var html=await c.GetStringAsync("/"+modulo);
        var n=g.Requests.Count;
        var r=await c.PostAsync("/"+modulo+"?handler=Detalle",new FormUrlEncodedContent(new Dictionary<string,string>{["id"]=id,["__RequestVerificationToken"]=Csrf(html)}));
        Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);Assert.Equal(n,g.Requests.Count);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task ValidacionServidor_ConservaValoresYBloqueaEscritura(bool editar)
    {
        using var g=new Gateway();await using var f=Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await Login(c,"Cajero");var html=await c.GetStringAsync("/Clientes?Nuevo=true");
        var form=Form(html,false);form["Form.RazonSocial"]="   ";form["Form.Telefono"]="abc";if(editar)form["EditarId"]="2";
        var r=await c.PostAsync("/Clientes?handler=Guardar",new FormUrlEncodedContent(form));
        html=WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());Assert.Contains("La razón social es obligatoria",html);Assert.Contains("value=\"abc\"",html);
        Assert.DoesNotContain(g.Requests,x=>x.Path.StartsWith("/api/clientes")&&x.Method!="GET");
    }

}
