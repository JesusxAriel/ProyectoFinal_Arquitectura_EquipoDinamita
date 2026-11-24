using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Time.Testing;
using PuntoMarket.Frontend.Auth;
using PuntoMarket.Frontend.Compartido;
using PuntoMarket.Frontend.Pages.Auth;

namespace PuntoMarket.Frontend.Tests.Auth;

public sealed class CambiarPasswordPagesTests
{
    private sealed class PerfilFalso : IPerfilApiAdapter
    {
        public Result Respuesta { get; set; } = Result.Ok();
        public List<CambiarPasswordRequest> Requests { get; } = [];

        public Task<Result> CambiarPassword(CambiarPasswordRequest request)
        {
            Requests.Add(request);
            return Task.FromResult(Respuesta);
        }
    }

    private readonly DefaultHttpContext _context = new() { Session = new FakeSession() };
    private readonly FakeTimeProvider _reloj = new(DateTimeOffset.Parse("2026-11-19T10:00:00Z"));
    private readonly PerfilFalso _perfil = new();

    private SesionUsuario CrearSesion(bool? obligatorio)
    {
        var sesion = new SesionUsuario(new HttpContextAccessor { HttpContext = _context }, TokenProvider.Instance, _reloj);
        if (obligatorio.HasValue)
            sesion.Iniciar(new("jwt-prueba", 1800, "ana", "Ana Prueba", "Cajero", obligatorio.Value));
        return sesion;
    }

    private CambiarPasswordPageModel CrearPagina(bool obligatorio, ISesionUsuario sesion, string nueva = "Nueva#12") => obligatorio
        ? new CambiarPasswordObligatorioModel(sesion, _perfil)
        {
            PasswordActual = "Actual#12", PasswordNueva = nueva, RepetirPasswordNueva = nueva
        }
        : new CambiarPasswordModel(sesion, _perfil)
        {
            PasswordActual = "Actual#12", PasswordNueva = nueva, RepetirPasswordNueva = nueva
        };

    private static Task<IActionResult> Post(PageModel pagina) => pagina switch
    {
        CambiarPasswordObligatorioModel p => p.OnPostAsync(),
        CambiarPasswordModel p => p.OnPostAsync(),
        _ => throw new InvalidOperationException()
    };

    private static IActionResult Get(PageModel pagina) => pagina switch
    {
        CambiarPasswordObligatorioModel p => p.OnGet(),
        CambiarPasswordModel p => p.OnGet(),
        _ => throw new InvalidOperationException()
    };

    private static void CargarModelState(CambiarPasswordPageModel pagina)
    {
        pagina.ModelState.SetModelValue(nameof(pagina.PasswordActual), pagina.PasswordActual, pagina.PasswordActual);
        pagina.ModelState.SetModelValue(nameof(pagina.PasswordNueva), pagina.PasswordNueva, pagina.PasswordNueva);
        pagina.ModelState.SetModelValue(nameof(pagina.RepetirPasswordNueva), pagina.RepetirPasswordNueva, pagina.RepetirPasswordNueva);
        pagina.ModelState.AddModelError(string.Empty, "Error anterior");
    }

    private static void VerificarPasswordsLimpias(CambiarPasswordPageModel pagina)
    {
        Assert.Equal("", pagina.PasswordActual);
        Assert.Equal("", pagina.PasswordNueva);
        Assert.Equal("", pagina.RepetirPasswordNueva);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ValidacionFallida_MuestraMensajeSinLlamarAdapter(bool obligatorio)
    {
        var sesion = CrearSesion(obligatorio);
        var anterior = sesion.Actual;
        var pagina = CrearPagina(obligatorio, sesion, "corta");
        CargarModelState(pagina);

        Assert.IsType<PageResult>(await Post(pagina));

        Assert.False(pagina.ModelState.ContainsKey(string.Empty));
        Assert.Equal("La nueva contraseña debe tener al menos 8 caracteres.",
            Assert.Single(pagina.ModelState[nameof(pagina.PasswordNueva)]!.Errors).ErrorMessage);
        Assert.Empty(_perfil.Requests);
        Assert.Equal(anterior, sesion.Actual);
        VerificarPasswordsLimpias(pagina);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AdapterFail_PasswordActualIncorrecta_SeMuestraBajoSuCampoSinCerrarSesion(bool obligatorio)
    {
        var sesion = CrearSesion(obligatorio);
        var anterior = sesion.Actual;
        _perfil.Respuesta = Result.Fail(MensajesAuth.PasswordActualIncorrecta);
        var pagina = CrearPagina(obligatorio, sesion);

        Assert.IsType<PageResult>(await Post(pagina));

        Assert.Equal("La contraseña actual no es correcta.",
            Assert.Single(pagina.ModelState[nameof(pagina.PasswordActual)]!.Errors).ErrorMessage);
        Assert.False(pagina.ModelState.ContainsKey(string.Empty));
        Assert.Equal(anterior, sesion.Actual);
        Assert.Equal("jwt-prueba", TokenProvider.Instance.Obtener(_context.Session));
        VerificarPasswordsLimpias(pagina);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AdapterFail_MuestraMensajeYMantieneSesion(bool obligatorio)
    {
        var sesion = CrearSesion(obligatorio);
        var anterior = sesion.Actual;
        _perfil.Respuesta = Result.Fail("No se pudo cambiar la contraseña.");
        var pagina = CrearPagina(obligatorio, sesion);
        CargarModelState(pagina);

        Assert.IsType<PageResult>(await Post(pagina));

        Assert.Equal(_perfil.Respuesta.Error, Assert.Single(pagina.ModelState[string.Empty]!.Errors).ErrorMessage);
        Assert.Equal(new CambiarPasswordRequest("Actual#12", "Nueva#12", "Nueva#12"), Assert.Single(_perfil.Requests));
        Assert.Equal(anterior, sesion.Actual);
        Assert.Equal("jwt-prueba", TokenProvider.Instance.Obtener(_context.Session));
        VerificarPasswordsLimpias(pagina);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AdapterOk_MarcaCambioYRedirigeAInicioSinCerrarSesion(bool obligatorio)
    {
        var sesion = CrearSesion(obligatorio);
        var anterior = sesion.Actual!;
        _reloj.Advance(TimeSpan.FromMinutes(5));
        var pagina = CrearPagina(obligatorio, sesion);
        CargarModelState(pagina);

        var resultado = Assert.IsType<RedirectToPageResult>(await Post(pagina));

        Assert.Equal(RutasAuth.Inicio, resultado.PageName);
        Assert.Equal(new CambiarPasswordRequest("Actual#12", "Nueva#12", "Nueva#12"), Assert.Single(_perfil.Requests));
        Assert.Equal(anterior with { DebeCambiarPassword = false }, sesion.Actual);
        Assert.Equal("jwt-prueba", TokenProvider.Instance.Obtener(_context.Session));
        VerificarPasswordsLimpias(pagina);
        Assert.Empty(pagina.ModelState);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SinSesion_GetYPostRedirigenALoginSinLlamarAdapter(bool obligatorio)
    {
        var pagina = CrearPagina(obligatorio, CrearSesion(null));

        Assert.Equal(RutasAuth.Login, Assert.IsType<RedirectToPageResult>(Get(pagina)).PageName);
        Assert.Equal(RutasAuth.Login, Assert.IsType<RedirectToPageResult>(await Post(pagina)).PageName);
        Assert.Empty(_perfil.Requests);
    }

    [Fact]
    public async Task Voluntario_CambioPendiente_GetYPostRedirigenAObligatorio()
    {
        var sesion = CrearSesion(true);
        var pagina = CrearPagina(false, sesion);

        Assert.Equal(RutasAuth.CambiarPasswordObligatorio, Assert.IsType<RedirectToPageResult>(Get(pagina)).PageName);
        Assert.Equal(RutasAuth.CambiarPasswordObligatorio, Assert.IsType<RedirectToPageResult>(await Post(pagina)).PageName);
        Assert.Empty(_perfil.Requests);
        Assert.True(sesion.Actual!.DebeCambiarPassword);
    }

    [Fact]
    public async Task Obligatorio_SinCambioPendiente_GetYPostRedirigenAInicio()
    {
        var sesion = CrearSesion(false);
        var anterior = sesion.Actual;
        var pagina = CrearPagina(true, sesion);

        Assert.Equal(RutasAuth.Inicio, Assert.IsType<RedirectToPageResult>(Get(pagina)).PageName);
        Assert.Equal(RutasAuth.Inicio, Assert.IsType<RedirectToPageResult>(await Post(pagina)).PageName);
        Assert.Empty(_perfil.Requests);
        Assert.Equal(anterior, sesion.Actual);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SesionPermitida_GetMuestraFormulario(bool obligatorio)
    {
        var pagina = CrearPagina(obligatorio, CrearSesion(obligatorio));

        Assert.IsType<PageResult>(Get(pagina));
        Assert.Empty(_perfil.Requests);
    }
}
