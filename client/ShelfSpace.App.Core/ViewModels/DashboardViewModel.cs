using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShelfSpace.App.Core.Services;

namespace ShelfSpace.App.Core.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [ObservableProperty]
    private int totalProducts;

    [ObservableProperty]
    private int lowStockCount;

    [ObservableProperty]
    private int totalOrders;

    [ObservableProperty]
    private int pendingOrders;

    [ObservableProperty]
    private decimal totalRevenue;

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
            var summary = await _dashboardService.GetSummaryAsync();
            TotalProducts = summary.TotalProducts;
            LowStockCount = summary.LowStockCount;
            TotalOrders = summary.TotalOrders;
            PendingOrders = summary.PendingOrders;
            TotalRevenue = summary.TotalRevenue;
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
}
