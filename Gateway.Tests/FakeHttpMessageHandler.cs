using System.Net;
using System.Text;

namespace PuntoMarket.Gateway.Tests;

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
        new(_ => JsonResponse(status, json));

    public static FakeHttpMessageHandler Throwing() =>
        new(_ => throw new HttpRequestException("sin conexión"));

    public static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
