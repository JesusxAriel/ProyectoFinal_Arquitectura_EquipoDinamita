using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PuntoMarket.Usuarios.Api.Filters;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Seguridad;

public sealed class GestionExceptionFilterTests
{
    private sealed record Registro(LogLevel Nivel, Exception? Excepcion, string Mensaje);

    private sealed class RegistroLogger<T> : ILogger<T>
    {
        public List<Registro> Registros { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Registros.Add(new Registro(logLevel, exception, formatter(state, exception)));
    }

    private static (ExceptionContext Contexto, RegistroLogger<GestionExceptionFilterAttribute> Logger) Crear(Exception excepcion)
    {
        var logger = new RegistroLogger<GestionExceptionFilterAttribute>();
        var servicios = new ServiceCollection().AddSingleton<ILogger<GestionExceptionFilterAttribute>>(logger).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = servicios };
        var accion = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return (new ExceptionContext(accion, []) { Exception = excepcion }, logger);
    }

    [Fact]
    public void OperacionInvalida_RegistraWarningConLaExcepcionYResponde409()
    {
        var excepcion = new InvalidOperationException("Falla de configuración");
        var (contexto, logger) = Crear(excepcion);

        new GestionExceptionFilterAttribute(StatusCodes.Status409Conflict).OnException(contexto);

        var registro = Assert.Single(logger.Registros);
        Assert.Equal(LogLevel.Warning, registro.Nivel);
        Assert.Same(excepcion, registro.Excepcion);
        Assert.Equal(409, ((ObjectResult)contexto.Result!).StatusCode);
        Assert.True(contexto.ExceptionHandled);
    }

    [Fact]
    public void ArgumentoInvalido_RegistraWarningYResponde400()
    {
        var (contexto, logger) = Crear(new ArgumentException("dato"));

        new GestionExceptionFilterAttribute(StatusCodes.Status409Conflict).OnException(contexto);

        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Registros).Nivel);
        Assert.Equal(400, ((ObjectResult)contexto.Result!).StatusCode);
    }

    [Fact]
    public void ErrorInesperado_RegistraErrorYResponde500SinFiltrarElDetalle()
    {
        var (contexto, logger) = Crear(new Exception("detalle interno"));

        new GestionExceptionFilterAttribute(StatusCodes.Status409Conflict).OnException(contexto);

        Assert.Equal(LogLevel.Error, Assert.Single(logger.Registros).Nivel);
        var problema = Assert.IsType<ProblemDetails>(((ObjectResult)contexto.Result!).Value);
        Assert.Equal(500, problema.Status);
        Assert.DoesNotContain("detalle interno", problema.Title);
    }

    [Fact]
    public void MensajeDelLog_NoIncluyeElMensajeDeLaExcepcion()
    {
        var (contexto, logger) = Crear(new InvalidOperationException("secreto-123"));

        new GestionExceptionFilterAttribute(StatusCodes.Status409Conflict).OnException(contexto);

        Assert.DoesNotContain("secreto-123", Assert.Single(logger.Registros).Mensaje);
    }

    [Fact]
    public void NoAutorizado_ResponderForbidSinRegistrar()
    {
        var (contexto, logger) = Crear(new UnauthorizedAccessException());

        new GestionExceptionFilterAttribute(StatusCodes.Status409Conflict).OnException(contexto);

        Assert.IsType<ForbidResult>(contexto.Result);
        Assert.Empty(logger.Registros);
    }

    [Fact]
    public void Cancelacion_NoSeTrataNiSeRegistra()
    {
        var (contexto, logger) = Crear(new OperationCanceledException());

        new GestionExceptionFilterAttribute(StatusCodes.Status409Conflict).OnException(contexto);

        Assert.Null(contexto.Result);
        Assert.Empty(logger.Registros);
    }
}
