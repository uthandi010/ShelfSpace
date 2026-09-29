using ShelfSpace.App.Core.ViewModels;

namespace ShelfSpace.App.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.Registered += OnRegistered;
    }

    private void OnRegistered(object? sender, EventArgs e) => AppNavigator.ShowMainApp();
}
