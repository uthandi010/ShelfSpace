using System.Net.Http.Headers;
using System.Net.Http.Json;
using ShelfSpace.Api.Dtos;

namespace ShelfSpace.Api.Tests;

public static class TestClientExtensions
{
    public static async Task<AuthResponse> RegisterAsync(
        this HttpClient client, string name = "Test User", string? email = null, string password = "Password123!")
    {
        email ??= $"user{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(name, email, password));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!;
    }

    public static void AuthorizeAs(this HttpClient client, AuthResponse auth)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
    }
}
