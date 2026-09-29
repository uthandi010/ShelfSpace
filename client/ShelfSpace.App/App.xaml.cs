using Microsoft.Extensions.DependencyInjection;
using ShelfSpace.App.Pages;

namespace ShelfSpace.App;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        AppNavigator.ServiceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
        return new Window(new NavigationPage(loginPage)) { Title = "ShelfSpace" };
    }
}
