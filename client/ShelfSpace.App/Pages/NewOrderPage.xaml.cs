using ShelfSpace.App.Core.ViewModels;

namespace ShelfSpace.App.Pages;

public partial class NewOrderPage : ContentPage
{
    private readonly NewOrderViewModel _viewModel;

    public NewOrderPage(NewOrderViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadProductsCommand.Execute(null);
    }

    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NewOrderViewModel.CreatedOrder) && _viewModel.CreatedOrder is not null)
        {
            await DisplayAlert("Order placed", $"Order for {_viewModel.CreatedOrder.CustomerName} was created.", "OK");
            await Navigation.PopModalAsync();
        }
    }

    private void OnRemoveLineClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is OrderLineDraft line)
        {
            _viewModel.RemoveLineCommand.Execute(line);
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e) => await Navigation.PopModalAsync();
}
