using Microsoft.Extensions.DependencyInjection;
using ShelfSpace.App.Core.ViewModels;

namespace ShelfSpace.App.Pages;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        viewModel.LoggedIn += OnLoggedIn;
    }

    private void OnLoggedIn(object? sender, EventArgs e) => AppNavigator.ShowMainApp();

    private async void OnCreateAccountClicked(object sender, EventArgs e)
    {
        var registerPage = Handler!.MauiContext!.Services.GetRequiredService<RegisterPage>();
        await Navigation.PushAsync(registerPage);
    }
}
