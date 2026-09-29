using ShelfSpace.App.Core.Models;
using ShelfSpace.App.Core.Services;
using ShelfSpace.App.Core.ViewModels;
using Xunit;

namespace ShelfSpace.App.Tests;

public class DashboardViewModelTests
{
    [Fact]
    public async Task LoadCommand_PopulatesAllFieldsFromTheSummary()
    {
        var service = new FakeDashboardService
        {
            Summary = new DashboardSummary(TotalProducts: 12, LowStockCount: 3, TotalOrders: 40, PendingOrders: 5, TotalRevenueCents: 250000),
        };
        var vm = new DashboardViewModel(service);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(12, vm.TotalProducts);
        Assert.Equal(3, vm.LowStockCount);
        Assert.Equal(40, vm.TotalOrders);
        Assert.Equal(5, vm.PendingOrders);
        Assert.Equal(2500.00m, vm.TotalRevenue);
    }

    [Fact]
    public async Task LoadCommand_OnFailure_SetsErrorMessage()
    {
        var service = new FakeDashboardService { ThrowOnNextCall = new ApiException(500, "Server error.") };
        var vm = new DashboardViewModel(service);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Server error.", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadCommand_TogglesIsBusy()
    {
        var service = new FakeDashboardService();
        var vm = new DashboardViewModel(service);
        Assert.False(vm.IsBusy);

        var task = vm.LoadCommand.ExecuteAsync(null);
        await task;

        Assert.False(vm.IsBusy);
    }
}
