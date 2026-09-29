using System.Net;
using System.Text;

namespace ShelfSpace.App.Tests;

/// <summary>A stand-in HttpMessageHandler so ApiClient tests never touch a
/// real network - it just replays a canned response and records the last
/// request it was given.</summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string? _jsonBody;

    public HttpRequestMessage? LastRequest { get; private set; }

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string? jsonBody = null)
    {
        _statusCode = statusCode;
        _jsonBody = jsonBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;

        var response = new HttpResponseMessage(_statusCode);
        if (_jsonBody is not null)
        {
            response.Content = new StringContent(_jsonBody, Encoding.UTF8, "application/json");
        }

        return Task.FromResult(response);
    }
}
