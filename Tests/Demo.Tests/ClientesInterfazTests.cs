extern alias Frontend;
using System.Net;
using System.Text.RegularExpressions;

namespace Demo.Tests;

public class ClientesInterfazTests
{
    [Theory][InlineData("?Nuevo=true","Añadir cliente")][InlineData("?EditarId=2","Editar cliente")]
    public async Task ModalFormulario_EstructuraYValidacionVaciaNoRecibeFoco(string query,string titulo)
    {
        using var g=new ClientesFrontendTests.GatewayFake();await using var f=ClientesFrontendTests.Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await ClientesFrontendTests.Login(c,"Administrador");
        var html=WebUtility.HtmlDecode(await c.GetStringAsync("/Clientes"+query));
        var modal=Regex.Match(html,"<div class=\"modal fade\" id=\"clienteModal\"(.*?)<noscript>",RegexOptions.Singleline).Value;
        Assert.Contains("<span class=\"eyebrow\">DIRECTORIO DE CLIENTES</span>",modal);Assert.Contains(titulo,modal);
        Assert.Contains("class=\"modal-header\"><div>",modal);Assert.Contains("data-bs-dismiss=\"modal\"",modal);
        Assert.Contains("modal-body",modal);Assert.Contains("modal-footer",modal);Assert.Contains("bi-check2",modal);
        Assert.DoesNotContain("validation-summary-valid",modal);Assert.DoesNotContain("role=\"alert\"",modal);
        Assert.Contains("data-cliente-auto-open=\"true\"",modal);
    }
    [Theory][InlineData("Administrador")][InlineData("Cajero")]
    public async Task Detalle_EsModalSoloLectura_ConDatosOpcionalesVacios(string perfil)
    {
        using var g=new ClientesFrontendTests.GatewayFake {SinOpcionales=true};await using var f=ClientesFrontendTests.Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await ClientesFrontendTests.Login(c,perfil);
        var html=WebUtility.HtmlDecode(await c.GetStringAsync("/Clientes?DetalleId=1"));
        var detalle=Regex.Match(html,"id=\"clienteDetalleModal\"(.*?)<noscript>",RegexOptions.Singleline).Value;
        Assert.Contains("data-cliente-detalle-auto-open=\"true\"",detalle);Assert.Contains("Cliente genérico protegido",detalle);
        Assert.Equal(2,Regex.Matches(detalle,"No registrado").Count);Assert.Contains("NIT/CI",detalle);Assert.Contains("Razón social",detalle);Assert.Contains("Estado",detalle);
        Assert.DoesNotContain("<input",detalle);Assert.DoesNotContain("<form",detalle);Assert.DoesNotContain("EditarId",detalle);Assert.Contains("data-bs-dismiss=\"modal\"",detalle);
    }
    [Fact] public async Task Filtros_SoloDosOrdenes_YBusquedaSinBoton()
    {
        using var g=new ClientesFrontendTests.GatewayFake();await using var f=ClientesFrontendTests.Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await ClientesFrontendTests.Login(c,"Cajero");
        var html=WebUtility.HtmlDecode(await c.GetStringAsync("/Clientes"));
        var formulario=Regex.Match(html,"<form method=\"get\"(.*?)</form>",RegexOptions.Singleline).Value;
        var orden=Regex.Match(formulario,"<select[^>]*id=\"OrdenarPor\"(.*?)</select>",RegexOptions.Singleline).Value;
        Assert.Equal(2,Regex.Matches(orden,"<option").Count);Assert.Contains("RazonSocial",orden);Assert.Contains("NitCi",orden);
        Assert.DoesNotContain("FechaCreacion",formulario);Assert.DoesNotContain("FechaActualizacion",formulario);Assert.DoesNotContain("type=\"submit\"",formulario);
        var limpiar=Regex.Match(formulario,"<button[^>]*data-clientes-clear[^>]*>").Value;Assert.Contains("hidden",limpiar);
        html=WebUtility.HtmlDecode(await c.GetStringAsync("/Clientes?Busqueda=123&Pagina=2&OrdenarPor=NitCi"));
        limpiar=Regex.Match(html,"<button[^>]*data-clientes-clear[^>]*>").Value;Assert.DoesNotContain("hidden",limpiar);
        Assert.Contains(g.Peticiones,p=>p.Path.Contains("busqueda=123") && p.Path.Contains("pagina=2") && p.Path.Contains("ordenarPor=NitCi"));
    }
    [Theory][InlineData("Administrador",true)][InlineData("Cajero",false)]
    public async Task Iconos_Accesibles_YAccionesSegunPermisos(string perfil,bool admin)
    {
        using var g=new ClientesFrontendTests.GatewayFake();await using var f=ClientesFrontendTests.Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await ClientesFrontendTests.Login(c,perfil);
        var html=WebUtility.HtmlDecode(await c.GetStringAsync("/Clientes"));
        Assert.Contains("bi-eye",html);Assert.Contains("bi-pencil-square",html);Assert.Contains("aria-label=\"Ver detalles de",html);Assert.Contains("title=\"Editar",html);
        Assert.Equal(admin,html.Contains("bi-trash3"));
        html=WebUtility.HtmlDecode(await c.GetStringAsync("/Clientes?Estado=Inactivos"));Assert.Equal(admin,html.Contains("bi-arrow-counterclockwise"));
    }
    [Fact] public async Task SinCoincidencias_MensajeClaro()
    {
        using var g=new ClientesFrontendTests.GatewayFake {ListadoVacio=true};await using var f=ClientesFrontendTests.Crear(g);using var c=f.CreateClient(new(){AllowAutoRedirect=false});await ClientesFrontendTests.Login(c,"Administrador");
        var html=await c.GetStringAsync("/Clientes?Busqueda=inexistente");Assert.Contains("No hay coincidencias",html);
    }
}
