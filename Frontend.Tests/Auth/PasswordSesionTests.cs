using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using PuntoMarket.Frontend.Auth;

namespace PuntoMarket.Frontend.Tests.Auth;

public sealed class PasswordSesionTests
{
    private readonly FakeTimeProvider _reloj = new(DateTimeOffset.Parse("2026-11-19T10:00:00Z"));
    private readonly DefaultHttpContext _context = new() { Session = new FakeSession() };

    private SesionUsuario Crear() => new(new HttpContextAccessor { HttpContext = _context }, TokenProvider.Instance, _reloj);

    [Fact]
    public void MarcarPasswordCambiada_PersisteFalseYConservaIdentidadTokenYExpiracion()
    {
        ISesionUsuario sesion = Crear();
        sesion.Iniciar(new("jwt-prueba", 1800, "ana", "Ana Prueba", "Cajero", true));
        var anterior = sesion.Actual!;
        _context.Session.SetString("otro.dato", "conservar");
        _reloj.Advance(TimeSpan.FromMinutes(5));

        sesion.MarcarPasswordCambiada();

        Assert.Equal(anterior with { DebeCambiarPassword = false }, Crear().Actual);
        Assert.Equal("jwt-prueba", TokenProvider.Instance.Obtener(_context.Session));
        Assert.Equal("conservar", _context.Session.GetString("otro.dato"));
    }

    [Fact]
    public void MarcarPasswordCambiada_SinSesion_NoCreaDatosNiToken()
    {
        Crear().MarcarPasswordCambiada();

        Assert.Null(Crear().Actual);
        Assert.Empty(_context.Session.Keys);
    }

    [Fact]
    public void MarcarPasswordCambiada_SesionVencida_NoLaModificaNiReactiva()
    {
        var sesion = Crear();
        sesion.Iniciar(new("jwt-prueba", 1800, "ana", "Ana Prueba", "Cajero", true));
        var datos = _context.Session.GetString("auth.datos");
        _reloj.Advance(TimeSpan.FromSeconds(1800));

        sesion.MarcarPasswordCambiada();

        Assert.Null(Crear().Actual);
        Assert.Equal(datos, _context.Session.GetString("auth.datos"));
        Assert.Equal("jwt-prueba", TokenProvider.Instance.Obtener(_context.Session));
    }
}
