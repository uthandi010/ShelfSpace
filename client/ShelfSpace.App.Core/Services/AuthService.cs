using ShelfSpace.App.Core.Models;

namespace ShelfSpace.App.Core.Services;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    Task<AuthResult> RegisterAsync(string name, string email, string password);
    Task<AuthResult> LoginAsync(string email, string password);
    void Logout();
}

public class AuthService : IAuthService
{
    private readonly ApiClient _apiClient;
    private readonly ITokenStore _tokenStore;

    public AuthService(ApiClient apiClient, ITokenStore tokenStore)
    {
        _apiClient = apiClient;
        _tokenStore = tokenStore;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_tokenStore.Token);

    public async Task<AuthResult> RegisterAsync(string name, string email, string password)
    {
        var result = await _apiClient.PostAsync<AuthResult>(
            "/api/auth/register", new { name, email, password }, auth: false);
        _tokenStore.Token = result.Token;
        return result;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var result = await _apiClient.PostAsync<AuthResult>(
            "/api/auth/login", new { email, password }, auth: false);
        _tokenStore.Token = result.Token;
        return result;
    }

    public void Logout() => _tokenStore.Token = null;
}
