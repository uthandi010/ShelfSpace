using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShelfSpace.App.Core.Models;
using ShelfSpace.App.Core.Services;

namespace ShelfSpace.App.Core.ViewModels;

public partial class OrdersViewModel : ObservableObject
{
    private readonly IOrderService _orderService;

    public OrdersViewModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public ObservableCollection<Order> Orders { get; } = new();

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var orders = await _orderService.ListAsync();
            Orders.Clear();
            foreach (var order in orders)
            {
                Orders.Add(order);
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task FulfillAsync(Order order)
    {
        ErrorMessage = null;
        try
        {
            var updated = await _orderService.FulfillAsync(order.Id);
            Replace(order, updated);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task CancelAsync(Order order)
    {
        ErrorMessage = null;
        try
        {
            var updated = await _orderService.CancelAsync(order.Id);
            Replace(order, updated);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void Replace(Order previous, Order updated)
    {
        var index = Orders.IndexOf(previous);
        if (index >= 0)
        {
            Orders[index] = updated;
        }
    }
}
