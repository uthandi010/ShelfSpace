using ShelfSpace.App.Core.Models;
using ShelfSpace.App.Core.ViewModels;
using Xunit;

namespace ShelfSpace.App.Tests;

public class NewOrderViewModelTests
{
    private static Product MakeProduct(int id, int priceCents = 1000, int quantity = 10) =>
        new(id, $"SKU-{id}", $"Product {id}", null, priceCents, quantity, 5, quantity <= 5);

    [Fact]
    public async Task LoadProductsCommand_OnlyListsInStockProducts()
    {
        var productService = new FakeProductService
        {
            Products = { MakeProduct(1, quantity: 10), MakeProduct(2, quantity: 0) },
        };
        var vm = new NewOrderViewModel(productService, new FakeOrderService());

        await vm.LoadProductsCommand.ExecuteAsync(null);

        Assert.Single(vm.AvailableProducts);
        Assert.Equal(1, vm.AvailableProducts[0].Id);
    }

    [Fact]
    public void AddLineCommand_WithNoProductSelected_ShowsError()
    {
        var vm = new NewOrderViewModel(new FakeProductService(), new FakeOrderService());

        vm.AddLineCommand.Execute(null);

        Assert.Equal("Choose a product first.", vm.ErrorMessage);
        Assert.Empty(vm.Lines);
    }

    [Fact]
    public void AddLineCommand_WithInvalidQuantity_ShowsError()
    {
        var vm = new NewOrderViewModel(new FakeProductService(), new FakeOrderService())
        {
            SelectedProduct = MakeProduct(1),
            QuantityText = "0",
        };

        vm.AddLineCommand.Execute(null);

        Assert.Equal("Enter a valid quantity.", vm.ErrorMessage);
    }

    [Fact]
    public void AddLineCommand_AddingTheSameProductTwice_MergesIntoOneLine()
    {
        var product = MakeProduct(1);
        var vm = new NewOrderViewModel(new FakeProductService(), new FakeOrderService())
        {
            SelectedProduct = product,
            QuantityText = "2",
        };

        vm.AddLineCommand.Execute(null);
        vm.SelectedProduct = product;
        vm.QuantityText = "3";
        vm.AddLineCommand.Execute(null);

        Assert.Single(vm.Lines);
        Assert.Equal(5, vm.Lines[0].Quantity);
    }

    [Fact]
    public void TotalPreview_ReflectsAllLines()
    {
        var vm = new NewOrderViewModel(new FakeProductService(), new FakeOrderService())
        {
            SelectedProduct = MakeProduct(1, priceCents: 1000),
            QuantityText = "2",
        };
        vm.AddLineCommand.Execute(null);
        vm.SelectedProduct = MakeProduct(2, priceCents: 500);
        vm.QuantityText = "1";
        vm.AddLineCommand.Execute(null);

        // 2 * $10.00 + 1 * $5.00
        Assert.Equal(25.00m, vm.TotalPreview);
    }

    [Fact]
    public void RemoveLineCommand_RemovesTheLine()
    {
        var vm = new NewOrderViewModel(new FakeProductService(), new FakeOrderService())
        {
            SelectedProduct = MakeProduct(1),
            QuantityText = "1",
        };
        vm.AddLineCommand.Execute(null);

        vm.RemoveLineCommand.Execute(vm.Lines[0]);

        Assert.Empty(vm.Lines);
        Assert.Equal(0m, vm.TotalPreview);
    }

    [Fact]
    public async Task SubmitCommand_WithNoCustomerName_ShowsErrorAndDoesNotCallService()
    {
        var orderService = new FakeOrderService();
        var vm = new NewOrderViewModel(new FakeProductService(), orderService)
        {
            SelectedProduct = MakeProduct(1),
            QuantityText = "1",
        };
        vm.AddLineCommand.Execute(null);

        await vm.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("Customer name is required.", vm.ErrorMessage);
        Assert.Empty(orderService.CreateCalls);
    }

    [Fact]
    public async Task SubmitCommand_WithNoLines_ShowsError()
    {
        var vm = new NewOrderViewModel(new FakeProductService(), new FakeOrderService())
        {
            CustomerName = "Jane",
        };

        await vm.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("Add at least one line item.", vm.ErrorMessage);
    }

    [Fact]
    public async Task SubmitCommand_WithValidData_CreatesOrderWithAllLines()
    {
        var orderService = new FakeOrderService();
        var vm = new NewOrderViewModel(new FakeProductService(), orderService)
        {
            CustomerName = "Jane",
            SelectedProduct = MakeProduct(1),
            QuantityText = "2",
        };
        vm.AddLineCommand.Execute(null);
        vm.SelectedProduct = MakeProduct(2);
        vm.QuantityText = "3";
        vm.AddLineCommand.Execute(null);

        await vm.SubmitCommand.ExecuteAsync(null);

        Assert.NotNull(vm.CreatedOrder);
        var call = Assert.Single(orderService.CreateCalls);
        Assert.Equal("Jane", call.CustomerName);
        Assert.Equal(2, call.Items.Count);
    }
}
