extern alias Frontend;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Proveedores.Domain.Entities;

namespace Proveedores.Tests;

public class FrontendFormularioTests
{
    private sealed class Gateway : HttpMessageHandler, IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(this, false) { BaseAddress = new Uri("http://gateway.test/") };
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(
                r.RequestUri!.AbsolutePath == "/dev-auth/token"
                    ? """{"access_token":"demo","expires_in":3600}"""
                    : """{"items":[],"pagina":1,"tamanoPagina":8,"total":0,"totalPaginas":0}""",
                System.Text.Encoding.UTF8, "application/json") });
    }

    [Fact]
    public async Task FormularioRenderizado_RespetaLongitudesYCamposDelDominio()
    {
        using var gateway = new Gateway();
        await using var factory = new WebApplicationFactory<Frontend::Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureLogging(l => l.ClearProviders());
            b.ConfigureTestServices(s =>
            {
                s.RemoveAll<IHttpClientFactory>();
                s.AddSingleton<IHttpClientFactory>(gateway);
                // No persiste claves ni requiere acceso al perfil o DPAPI de Windows.
                s.RemoveAll<IDataProtectionProvider>();
                s.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            });
        });
        using var cliente = factory.CreateClient();
        var inicial = await cliente.GetStringAsync("/Login");
        var csrf = WebUtility.HtmlDecode(Regex.Match(inicial,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(csrf);
        using var cambio = await cliente.PostAsync("/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["perfilElegido"] = "Administrador", ["__RequestVerificationToken"] = csrf
        }));
        cambio.EnsureSuccessStatusCode();
        var html = await cliente.GetStringAsync("/Proveedores");
        string Input(string nombre) => Regex.Matches(html, "<input\\b[^>]*>")
            .Select(m => m.Value).Single(v => v.Contains($"name=\"Form.{nombre}\""));
        var nombre = Input("NombreEmpresa");
        var numero = Input("NumeroEmpresa");
        var correo = Input("CorreoReferencia");
        Assert.Contains($"maxlength=\"{Proveedor.NombreMaxLength}\"", nombre);
        Assert.Contains($"maxlength=\"{Proveedor.NumeroMaxLength}\"", numero);
        Assert.Contains($"maxlength=\"{Proveedor.CorreoMaxLength}\"", correo);
        Assert.Contains("required", nombre);
        Assert.Contains("required", numero);
        Assert.DoesNotContain("required", correo);
        Assert.Contains("type=\"email\"", correo);
    }
}
