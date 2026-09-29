using Microsoft.Extensions.Logging;
using ShelfSpace.App.Core.Services;
using ShelfSpace.App.Core.ViewModels;
using ShelfSpace.App.Pages;
using ShelfSpace.App.Services;

namespace ShelfSpace.App;

public static class MauiProgram
{
    // Override with the SHELFSPACE_API_URL environment variable if the API
    // isn't running on the default port (see server/ShelfSpace.Api's
    // launchSettings.json).
    private static string ApiBaseUrl =>
        Environment.GetEnvironmentVariable("SHELFSPACE_API_URL") ?? "http://localhost:5098";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ----- Services -----
        builder.Services.AddSingleton<ITokenStore, PreferencesTokenStore>();
        builder.Services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(ApiBaseUrl) });
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IProductService, ProductService>();
        builder.Services.AddSingleton<IOrderService, OrderService>();
        builder.Services.AddSingleton<IDashboardService, DashboardService>();

        // ----- ViewModels -----
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ProductsViewModel>();
        builder.Services.AddTransient<OrdersViewModel>();
        builder.Services.AddTransient<NewOrderViewModel>();

        // ----- Pages -----
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ProductsPage>();
        builder.Services.AddTransient<OrdersPage>();
        builder.Services.AddTransient<NewOrderPage>();
        builder.Services.AddTransient<MainTabbedPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
