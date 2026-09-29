using Microsoft.Extensions.DependencyInjection;
using ShelfSpace.App.Core.Services;
using ShelfSpace.App.Pages;

namespace ShelfSpace.App;

/// <summary>Centralizes switching between the logged-out (Login) and
/// logged-in (tabbed) shells of the app, so pages don't each need their own
/// copy of this logic.</summary>
public static class AppNavigator
{
    public static IServiceProvider ServiceProvider { get; set; } = null!;

    public static void ShowMainApp()
    {
        var mainPage = ServiceProvider.GetRequiredService<MainTabbedPage>();
        SetRootPage(mainPage);
    }

    public static void ShowLogin()
    {
        var loginPage = ServiceProvider.GetRequiredService<LoginPage>();
        SetRootPage(new NavigationPage(loginPage));
    }

    public static void Logout()
    {
        ServiceProvider.GetRequiredService<IAuthService>().Logout();
        ShowLogin();
    }

    private static void SetRootPage(Microsoft.Maui.Controls.Page page)
    {
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = page;
        }
    }
}
