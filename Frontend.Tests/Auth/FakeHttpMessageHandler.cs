using System.Net;
using System.Text;

namespace PuntoMarket.Frontend.Tests.Auth;

public sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(respond(request));
    }

    public static FakeHttpMessageHandler Returning(HttpStatusCode status, string json = "") =>
        new(_ => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    public static FakeHttpMessageHandler Throwing() =>
        new(_ => throw new HttpRequestException("sin conexión"));
}
