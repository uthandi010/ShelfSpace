using System.Net;
using ShelfSpace.App.Core.Services;
using Xunit;

namespace ShelfSpace.App.Tests;

public class ApiClientTests
{
    private static ApiClient BuildClient(FakeHttpMessageHandler handler, ITokenStore? tokenStore = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        return new ApiClient(httpClient, tokenStore ?? new InMemoryTokenStore());
    }

    [Fact]
    public async Task GetAsync_ReturnsDeserializedBody()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"id":1,"name":"Widget"}""");
        var client = BuildClient(handler);

        var result = await client.GetAsync<TestDto>("/api/things/1");

        Assert.Equal(1, result.Id);
        Assert.Equal("Widget", result.Name);
    }

    [Fact]
    public async Task Requests_DoNotAttachAuthorizationHeader_WhenNoTokenStored()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"id":1,"name":"Widget"}""");
        var client = BuildClient(handler);

        await client.GetAsync<TestDto>("/api/things/1");

        Assert.Null(handler.LastRequest!.Headers.Authorization);
    }

    [Fact]
    public async Task Requests_AttachBearerToken_WhenTokenStored()
    {
        var tokenStore = new InMemoryTokenStore { Token = "abc123" };
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"id":1,"name":"Widget"}""");
        var client = BuildClient(handler, tokenStore);

        await client.GetAsync<TestDto>("/api/things/1");

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("abc123", handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task PostAsync_WithAuthFalse_NeverAttachesToken()
    {
        var tokenStore = new InMemoryTokenStore { Token = "abc123" };
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"id":1,"name":"Widget"}""");
        var client = BuildClient(handler, tokenStore);

        await client.PostAsync<TestDto>("/api/auth/login", new { }, auth: false);

        Assert.Null(handler.LastRequest!.Headers.Authorization);
    }

    [Fact]
    public async Task FailedRequest_ThrowsApiExceptionWithServerMessage()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, """{"message":"Invalid email or password."}""");
        var client = BuildClient(handler);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<TestDto>("/api/things/1"));

        Assert.Equal("Invalid email or password.", ex.Message);
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task FailedRequest_WithNoJsonBody_FallsBackToGenericMessage()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError);
        var client = BuildClient(handler);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<TestDto>("/api/things/1"));

        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_TreatsNoContentAsSuccess()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NoContent);
        var client = BuildClient(handler);

        await client.DeleteAsync("/api/things/1"); // should not throw
    }

    private record TestDto(int Id, string Name);
}
