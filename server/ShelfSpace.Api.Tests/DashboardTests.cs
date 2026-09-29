using System.Net.Http.Json;
using ShelfSpace.Api.Dtos;
using Xunit;

namespace ShelfSpace.Api.Tests;

public class DashboardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DashboardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<ProductResponse> CreateProductAsync(
        HttpClient client, string sku, int priceCents, int quantity, int? threshold = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/products", new CreateProductRequest(sku, $"Product {sku}", null, priceCents, quantity, threshold));
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    [Fact]
    public async Task BrandNewAccount_HasAllZeroSummary()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());

        var summary = await client.GetFromJsonAsync<DashboardSummary>("/api/dashboard/summary");

        Assert.Equal(0, summary!.TotalProducts);
        Assert.Equal(0, summary.LowStockCount);
        Assert.Equal(0, summary.TotalOrders);
        Assert.Equal(0, summary.PendingOrders);
        Assert.Equal(0, summary.TotalRevenueCents);
    }

    [Fact]
    public async Task CountsProductsOrdersAndLowStockCorrectly()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        await CreateProductAsync(client, "SKU-LOW", 1000, 1, threshold: 5);
        var wellStocked = await CreateProductAsync(client, "SKU-OK", 1000, 100, threshold: 5);

        await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Jane", new List<CreateOrderItemRequest> { new(wellStocked.Id, 2) }));

        var summary = await client.GetFromJsonAsync<DashboardSummary>("/api/dashboard/summary");

        Assert.Equal(2, summary!.TotalProducts);
        Assert.Equal(1, summary.LowStockCount);
        Assert.Equal(1, summary.TotalOrders);
        Assert.Equal(1, summary.PendingOrders);
    }

    [Fact]
    public async Task RevenueOnlyCountsFulfilledOrders()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var product = await CreateProductAsync(client, "SKU-REV", 1000, 20);

        var pendingOrder = await (await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Jane", new List<CreateOrderItemRequest> { new(product.Id, 2) })))
            .Content.ReadFromJsonAsync<OrderResponse>();
        var fulfilledOrder = await (await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Bob", new List<CreateOrderItemRequest> { new(product.Id, 3) })))
            .Content.ReadFromJsonAsync<OrderResponse>();
        await client.PatchAsync($"/api/orders/{fulfilledOrder!.Id}/fulfill", null);

        var summary = await client.GetFromJsonAsync<DashboardSummary>("/api/dashboard/summary");

        // Only the fulfilled order's 3 units count toward revenue, not the
        // still-pending order's 2 units.
        Assert.Equal(3000, summary!.TotalRevenueCents);
        Assert.NotNull(pendingOrder);
    }

    [Fact]
    public async Task DashboardIsScopedToTheCurrentUser()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        await CreateProductAsync(client, "SKU-OTHER", 1000, 10);

        client.AuthorizeAs(await client.RegisterAsync());
        var summary = await client.GetFromJsonAsync<DashboardSummary>("/api/dashboard/summary");

        Assert.Equal(0, summary!.TotalProducts);
    }
}
