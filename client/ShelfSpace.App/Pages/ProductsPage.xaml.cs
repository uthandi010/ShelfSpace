using ShelfSpace.App.Core.Models;
using ShelfSpace.App.Core.ViewModels;

namespace ShelfSpace.App.Pages;

public partial class ProductsPage : ContentPage
{
    private readonly ProductsViewModel _viewModel;

    public ProductsPage(ProductsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }

    private async void OnRestockClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Product product)
        {
            return;
        }

        var input = await DisplayPromptAsync(
            "Restock", $"How many more units of \"{product.Name}\" came in?", initialValue: "10", keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        if (!int.TryParse(input, out var quantity) || quantity <= 0)
        {
            await DisplayAlert("Invalid quantity", "Enter a positive whole number.", "OK");
            return;
        }

        await _viewModel.RestockAsync(product.Id, quantity);
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Product product)
        {
            return;
        }

        var confirmed = await DisplayAlert("Delete product", $"Delete \"{product.Name}\"?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        await _viewModel.DeleteCommand.ExecuteAsync(product);
    }

    private void OnLogoutClicked(object sender, EventArgs e) => AppNavigator.Logout();
}
