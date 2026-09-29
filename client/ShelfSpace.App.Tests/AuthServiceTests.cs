using System.Net;
using ShelfSpace.App.Core.Services;
using Xunit;

namespace ShelfSpace.App.Tests;

public class AuthServiceTests
{
    private static (AuthService Service, InMemoryTokenStore TokenStore) Build(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var tokenStore = new InMemoryTokenStore();
        var apiClient = new ApiClient(httpClient, tokenStore);
        return (new AuthService(apiClient, tokenStore), tokenStore);
    }

    [Fact]
    public void IsAuthenticated_IsFalse_WhenNoTokenStored()
    {
        var (service, _) = Build(new FakeHttpMessageHandler(HttpStatusCode.OK));
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_StoresTheReturnedToken()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """{"token":"jwt-abc","userId":1,"name":"Ada","email":"ada@example.com"}""");
        var (service, tokenStore) = Build(handler);

        var result = await service.LoginAsync("ada@example.com", "Password123!");

        Assert.Equal("jwt-abc", tokenStore.Token);
        Assert.Equal("Ada", result.Name);
        Assert.True(service.IsAuthenticated);
    }

    [Fact]
    public async Task RegisterAsync_StoresTheReturnedToken()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """{"token":"jwt-xyz","userId":2,"name":"Grace","email":"grace@example.com"}""");
        var (service, tokenStore) = Build(handler);

        await service.RegisterAsync("Grace", "grace@example.com", "Password123!");

        Assert.Equal("jwt-xyz", tokenStore.Token);
    }

    [Fact]
    public async Task LoginAsync_WithBadCredentials_ThrowsAndLeavesNoTokenStored()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, """{"message":"Invalid email or password."}""");
        var (service, tokenStore) = Build(handler);

        await Assert.ThrowsAsync<ApiException>(() => service.LoginAsync("ada@example.com", "wrong"));

        Assert.Null(tokenStore.Token);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void Logout_ClearsTheStoredToken()
    {
        var (service, tokenStore) = Build(new FakeHttpMessageHandler(HttpStatusCode.OK));
        tokenStore.Token = "some-token";

        service.Logout();

        Assert.Null(tokenStore.Token);
        Assert.False(service.IsAuthenticated);
    }
}
