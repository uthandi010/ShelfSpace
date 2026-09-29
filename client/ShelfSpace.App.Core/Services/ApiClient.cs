using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShelfSpace.App.Core.Services;

public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>Reads/writes the JWT. The MAUI head provides the real
/// implementation (backed by Preferences); tests use an in-memory one.</summary>
public interface ITokenStore
{
    string? Token { get; set; }
}

public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ITokenStore _tokenStore;

    public ApiClient(HttpClient httpClient, ITokenStore tokenStore)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
    }

    public async Task<TResponse> GetAsync<TResponse>(string path)
    {
        var result = await SendAsync<TResponse>(HttpMethod.Get, path);
        return result ?? throw new ApiException(0, "The server returned an empty response.");
    }

    public async Task<TResponse> PostAsync<TResponse>(string path, object? body = null, bool auth = true)
    {
        var result = await SendAsync<TResponse>(HttpMethod.Post, path, body, auth);
        return result ?? throw new ApiException(0, "The server returned an empty response.");
    }

    public async Task<TResponse> PatchAsync<TResponse>(string path, object? body = null)
    {
        var result = await SendAsync<TResponse>(HttpMethod.Patch, path, body);
        return result ?? throw new ApiException(0, "The server returned an empty response.");
    }

    public Task DeleteAsync(string path) => SendAsync<object>(HttpMethod.Delete, path);

    private async Task<TResponse?> SendAsync<TResponse>(
        HttpMethod method, string path, object? body = null, bool auth = true)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }
        if (auth && !string.IsNullOrEmpty(_tokenStore.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.Token);
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(0, $"Could not reach the server: {ex.Message}");
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return default;
            }

            var text = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var message = $"Request failed with status {(int)response.StatusCode}";
                if (!string.IsNullOrWhiteSpace(text))
                {
                    try
                    {
                        var problem = JsonSerializer.Deserialize<JsonElement>(text, JsonOptions);
                        if (problem.ValueKind == JsonValueKind.Object &&
                            problem.TryGetProperty("message", out var messageProp) &&
                            messageProp.ValueKind == JsonValueKind.String)
                        {
                            message = messageProp.GetString() ?? message;
                        }
                    }
                    catch (JsonException)
                    {
                        // Not JSON (or not shaped as expected) - fall back to the generic message.
                    }
                }
                throw new ApiException((int)response.StatusCode, message);
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return default;
            }

            return JsonSerializer.Deserialize<TResponse>(text, JsonOptions);
        }
    }
}
