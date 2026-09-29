using ShelfSpace.App.Core.Services;

namespace ShelfSpace.App.Services;

/// <summary>Persists the JWT across app restarts using MAUI's Preferences
/// API, so the user doesn't have to log in again every time they open the
/// app. Not full SecureStorage-grade encryption, but reasonable for a
/// desktop line-of-business tool like this one.</summary>
public class PreferencesTokenStore : ITokenStore
{
    private const string Key = "auth_token";

    public string? Token
    {
        get => Preferences.Default.Get(Key, (string?)null);
        set
        {
            if (value is null)
            {
                Preferences.Default.Remove(Key);
            }
            else
            {
                Preferences.Default.Set(Key, value);
            }
        }
    }
}
