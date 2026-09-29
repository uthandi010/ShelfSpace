using System.Net;
using System.Net.Http.Json;
using ShelfSpace.Api.Dtos;
using Xunit;

namespace ShelfSpace.Api.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsTokenAndUser()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("Ada Lovelace", "ada@example.com", "Password123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.Equal("ada@example.com", auth.Email);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        await client.RegisterAsync(email: "duplicate@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("Someone Else", "duplicate@example.com", "Password123!"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("Short Pw", "shortpw@example.com", "1234"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        var client = _factory.CreateClient();
        await client.RegisterAsync(email: "grace@example.com", password: "Password123!");

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("grace@example.com", "Password123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        await client.RegisterAsync(email: "wrongpw@example.com", password: "Password123!");

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("wrongpw@example.com", "NotThePassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
