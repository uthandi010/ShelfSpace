using Microsoft.Extensions.DependencyInjection;
using ShelfSpace.App.Core.Models;
using ShelfSpace.App.Core.ViewModels;

namespace ShelfSpace.App.Pages;

public partial class OrdersPage : ContentPage
{
    private readonly OrdersViewModel _viewModel;

    public OrdersPage(OrdersViewModel viewModel)
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

    private async void OnNewOrderClicked(object sender, EventArgs e)
    {
        var newOrderPage = Handler!.MauiContext!.Services.GetRequiredService<NewOrderPage>();
        await Navigation.PushModalAsync(new NavigationPage(newOrderPage));
    }

    private async void OnFulfillClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is Order order)
        {
            await _viewModel.FulfillCommand.ExecuteAsync(order);
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Order order)
        {
            return;
        }

        var confirmed = await DisplayAlert("Cancel order", $"Cancel the order for \"{order.CustomerName}\"? This restores the stock.", "Cancel order", "Keep it");
        if (confirmed)
        {
            await _viewModel.CancelCommand.ExecuteAsync(order);
        }
    }

    private void OnLogoutClicked(object sender, EventArgs e) => AppNavigator.Logout();
}
