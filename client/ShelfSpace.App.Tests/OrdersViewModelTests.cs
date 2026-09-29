using ShelfSpace.App.Core.Models;
using ShelfSpace.App.Core.ViewModels;
using Xunit;

namespace ShelfSpace.App.Tests;

public class OrdersViewModelTests
{
    private static Order MakeOrder(int id, string status = "Pending") =>
        new(id, "Jane Customer", status, DateTime.UtcNow, 1000, new List<OrderItem>());

    [Fact]
    public async Task LoadCommand_PopulatesOrders()
    {
        var service = new FakeOrderService { Orders = { MakeOrder(1) } };
        var vm = new OrdersViewModel(service);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Single(vm.Orders);
    }

    [Fact]
    public async Task FulfillCommand_UpdatesStatusInPlace()
    {
        var service = new FakeOrderService { Orders = { MakeOrder(1) } };
        var vm = new OrdersViewModel(service);
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.FulfillCommand.ExecuteAsync(vm.Orders[0]);

        Assert.Equal("Fulfilled", vm.Orders[0].Status);
        Assert.Single(vm.Orders); // replaced, not duplicated
    }

    [Fact]
    public async Task CancelCommand_UpdatesStatusInPlace()
    {
        var service = new FakeOrderService { Orders = { MakeOrder(1) } };
        var vm = new OrdersViewModel(service);
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.CancelCommand.ExecuteAsync(vm.Orders[0]);

        Assert.Equal("Cancelled", vm.Orders[0].Status);
    }

    [Fact]
    public async Task FulfillCommand_OnFailure_SetsErrorMessage()
    {
        var service = new FakeOrderService { Orders = { MakeOrder(1) } };
        var vm = new OrdersViewModel(service);
        await vm.LoadCommand.ExecuteAsync(null);

        // Only throw on the *next* call, i.e. the fulfill itself, not the load above.
        service.ThrowOnNextCall = new ShelfSpace.App.Core.Services.ApiException(400, "Only a pending order can be fulfilled.");
        await vm.FulfillCommand.ExecuteAsync(vm.Orders[0]);

        Assert.Equal("Only a pending order can be fulfilled.", vm.ErrorMessage);
    }
}
