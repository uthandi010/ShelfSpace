using ShelfSpace.App.Core.Models;
using ShelfSpace.App.Core.Services;
using ShelfSpace.App.Core.ViewModels;
using Xunit;

namespace ShelfSpace.App.Tests;

public class ProductsViewModelTests
{
    [Fact]
    public async Task LoadCommand_PopulatesProducts()
    {
        var service = new FakeProductService
        {
            Products = { new Product(1, "SKU-1", "Widget", null, 1000, 10, 5, false) },
        };
        var vm = new ProductsViewModel(service);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Single(vm.Products);
        Assert.Equal("Widget", vm.Products[0].Name);
    }

    [Fact]
    public async Task LoadCommand_OnFailure_SetsErrorMessage()
    {
        var service = new FakeProductService { ThrowOnNextCall = new ApiException(500, "Server error.") };
        var vm = new ProductsViewModel(service);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Server error.", vm.ErrorMessage);
    }

    [Fact]
    public async Task AddProductCommand_WithMissingSku_ShowsErrorAndDoesNotCallService()
    {
        var service = new FakeProductService();
        var vm = new ProductsViewModel(service) { NewName = "Widget", NewPrice = "10", NewQuantity = "5" };

        await vm.AddProductCommand.ExecuteAsync(null);

        Assert.Equal("SKU and name are required.", vm.ErrorMessage);
        Assert.Empty(service.Products);
    }

    [Fact]
    public async Task AddProductCommand_WithInvalidPrice_ShowsError()
    {
        var service = new FakeProductService();
        var vm = new ProductsViewModel(service)
        {
            NewSku = "SKU-1", NewName = "Widget", NewPrice = "not-a-number", NewQuantity = "5",
        };

        await vm.AddProductCommand.ExecuteAsync(null);

        Assert.Equal("Enter a valid, non-negative price.", vm.ErrorMessage);
    }

    [Fact]
    public async Task AddProductCommand_WithValidData_ConvertsDollarsToCentsAndAddsToList()
    {
        var service = new FakeProductService();
        var vm = new ProductsViewModel(service)
        {
            NewSku = "SKU-1", NewName = "Widget", NewPrice = "19.99", NewQuantity = "10",
        };

        await vm.AddProductCommand.ExecuteAsync(null);

        Assert.Single(vm.Products);
        Assert.Equal(1999, vm.Products[0].PriceCents);
        Assert.Equal(string.Empty, vm.NewSku);
    }

    [Fact]
    public async Task RestockAsync_UpdatesTheMatchingProductInPlace()
    {
        var service = new FakeProductService
        {
            Products = { new Product(1, "SKU-1", "Widget", null, 1000, 5, 5, true) },
        };
        var vm = new ProductsViewModel(service);
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.RestockAsync(1, 20);

        Assert.Equal(25, vm.Products[0].QuantityOnHand);
        Assert.False(vm.Products[0].IsLowStock);
    }

    [Fact]
    public async Task DeleteCommand_RemovesTheProductFromTheList()
    {
        var service = new FakeProductService
        {
            Products = { new Product(1, "SKU-1", "Widget", null, 1000, 5, 5, true) },
        };
        var vm = new ProductsViewModel(service);
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.DeleteCommand.ExecuteAsync(vm.Products[0]);

        Assert.Empty(vm.Products);
    }
}
