extern alias Frontend;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Frontend::Frontend.Demo.Models;

namespace Demo.Tests;

public class ClientesNormalizacionTests
{
    [Theory]
    [InlineData("jesus ariel mamani gutierrez", "JESUS ARIEL MAMANI GUTIERREZ")]
    [InlineData("  juan   perez   vargas  ", "JUAN PEREZ VARGAS")]
    [InlineData("empresa   los andes s.r.l.", "EMPRESA LOS ANDES S.R.L.")]
    [InlineData("  compañía pérez & hijos ñ  ", "COMPAÑÍA PÉREZ & HIJOS Ñ")]
    [InlineData("comercial\u00a0\u00a0peña", "COMERCIAL PEÑA")]
    [InlineData("pen\u0303a", "PEN\u0303A")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Modelo_NormalizaAntesDeValidar(string? entrada, string esperado)
    {
        var form = new ClienteFormVm { NitCi = "12345", RazonSocial = entrada };
        Assert.Equal(esperado, form.RazonSocial);
        var errores = form.Validate(new ValidationContext(form)).ToArray();
        Assert.Equal(esperado.Length == 0, errores.Any(e => e.MemberNames.Contains("RazonSocial")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CrearYEditar_NormalizanSinJavascript_YConservanErrores(bool editar)
    {
        using var gateway = new ClientesFrontendTests.GatewayFake();
        await using var factory = ClientesFrontendTests.Crear(gateway);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await ClientesFrontendTests.Login(client, "Administrador");
        var html = WebUtility.HtmlDecode(await client.GetStringAsync(editar ? "/Clientes?EditarId=2" : "/Clientes?Nuevo=true"));
        foreach (var placeholder in new[] { "Ej. 6488615", "Ej. JUAN PEREZ VARGAS o EMPRESA LOS ANDES S.R.L.", "Ej. contacto@empresa.com", "Ej. 77123456" })
        {
            Assert.Contains("placeholder=\"" + placeholder + "\"", html);
            Assert.DoesNotContain("value=\"" + placeholder + "\"", html);
        }
        var url = "/Clientes?handler=Guardar" + (editar ? "&EditarId=2" : "");
        var form = ClientesFrontendTests.Form(html);
        form["Form.RazonSocial"] = "  compañía   peña s.r.l.  ";
        form["Form.Telefono"] = "abc";
        var response = await client.PostAsync(url, new FormUrlEncodedContent(form));
        html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("value=\"COMPAÑÍA PEÑA S.R.L.\"", html);
        Assert.Contains("value=\"abc\"", html);
        Assert.Contains("El teléfono debe tener 8 dígitos", html);
        Assert.Contains("data-cliente-auto-open=\"true\"", html);
        Assert.DoesNotContain(gateway.Peticiones, p => p.Path.StartsWith("/api/clientes") && p.Method is "POST" or "PUT");
        form = ClientesFrontendTests.Form(html);
        form["Form.RazonSocial"] = "   ";
        response = await client.PostAsync(url, new FormUrlEncodedContent(form));
        html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("La razón social es obligatoria", html);
        Assert.DoesNotContain(gateway.Peticiones, p => p.Path.StartsWith("/api/clientes") && p.Method is "POST" or "PUT");
        form = ClientesFrontendTests.Form(html);
        form["Form.RazonSocial"] = "  compañía   peña s.r.l.  ";
        response = await client.PostAsync(url, new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var write = Assert.Single(gateway.Peticiones, p => p.Path.StartsWith("/api/clientes") && p.Method is "POST" or "PUT");
        Assert.Equal(editar ? "PUT" : "POST", write.Method);
        using var json = JsonDocument.Parse(write.Body);
        Assert.Equal("COMPAÑÍA PEÑA S.R.L.", json.RootElement.GetProperty("razonSocial").GetString());
    }
}
