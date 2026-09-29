using System.Net;
using System.Net.Http.Json;
using ShelfSpace.Api.Dtos;
using Xunit;

namespace ShelfSpace.Api.Tests;

public class OrdersTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrdersTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<ProductResponse> CreateProductAsync(
        HttpClient client, string sku, int priceCents, int quantity)
    {
        var response = await client.PostAsJsonAsync(
            "/api/products", new CreateProductRequest(sku, $"Product {sku}", null, priceCents, quantity, null));
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    [Fact]
    public async Task CreatingOrder_DecrementsStock()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var product = await CreateProductAsync(client, "SKU-A", 1000, 10);

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Jane Customer", new List<CreateOrderItemRequest> { new(product.Id, 3) }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Pending", order!.Status);
        Assert.Equal(3000, order.TotalCents);

        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/products");
        Assert.Equal(7, products!.Single(p => p.Id == product.Id).QuantityOnHand);
    }

    [Fact]
    public async Task CreatingOrder_WithMoreThanAvailableStock_IsRejectedAndDoesNotChangeStock()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var product = await CreateProductAsync(client, "SKU-B", 1000, 2);

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Jane Customer", new List<CreateOrderItemRequest> { new(product.Id, 5) }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/products");
        Assert.Equal(2, products!.Single(p => p.Id == product.Id).QuantityOnHand);
    }

    [Fact]
    public async Task CreatingOrder_WithOneOutOfStockLine_RejectsTheWholeOrder()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var inStock = await CreateProductAsync(client, "SKU-C1", 500, 10);
        var outOfStock = await CreateProductAsync(client, "SKU-C2", 500, 1);

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Jane Customer", new List<CreateOrderItemRequest>
            {
                new(inStock.Id, 2),
                new(outOfStock.Id, 5),
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Neither line should have been applied - it's all-or-nothing.
        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/products");
        Assert.Equal(10, products!.Single(p => p.Id == inStock.Id).QuantityOnHand);
    }

    [Fact]
    public async Task CreatingOrder_SnapshotsPriceAtOrderTime()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var product = await CreateProductAsync(client, "SKU-D", 1000, 10);

        var orderResponse = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Jane Customer", new List<CreateOrderItemRequest> { new(product.Id, 1) }));
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>();

        // Price goes up after the order was placed.
        await client.PatchAsJsonAsync($"/api/products/{product.Id}", new UpdateProductRequest(null, null, 5000, null));

        var refetched = await client.GetFromJsonAsync<List<OrderResponse>>("/api/orders");
        var sameOrder = refetched!.Single(o => o.Id == order!.Id);
        Assert.Equal(1000, sameOrder.Items[0].UnitPriceCentsAtOrder);
    }

    [Fact]
    public async Task CancellingAPendingOrder_RestoresStock()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var product = await CreateProductAsync(client, "SKU-E", 1000, 10);
        var order = await (await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Jane Customer", new List<CreateOrderItemRequest> { new(product.Id, 4) })))
            .Content.ReadFromJsonAsync<OrderResponse>();

        var cancelResponse = await client.PatchAsync($"/api/orders/{order!.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/products");
        Assert.Equal(10, products!.Single(p => p.Id == product.Id).QuantityOnHand);
    }

    [Fact]
    public async Task FulfillingAnOrder_DoesNotChangeStockAgain()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var product = await CreateProductAsync(client, "SKU-F", 1000, 10);
        var order = await (await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Jane Customer", new List<CreateOrderItemRequest> { new(product.Id, 4) })))
            .Content.ReadFromJsonAsync<OrderResponse>();

        var fulfillResponse = await client.PatchAsync($"/api/orders/{order!.Id}/fulfill", null);

        Assert.Equal(HttpStatusCode.OK, fulfillResponse.StatusCode);
        var fulfilled = await fulfillResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Fulfilled", fulfilled!.Status);

        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/products");
        Assert.Equal(6, products!.Single(p => p.Id == product.Id).QuantityOnHand);
    }

    [Fact]
    public async Task CannotCancelAnAlreadyFulfilledOrder()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var product = await CreateProductAsync(client, "SKU-G", 1000, 10);
        var order = await (await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Jane Customer", new List<CreateOrderItemRequest> { new(product.Id, 2) })))
            .Content.ReadFromJsonAsync<OrderResponse>();
        await client.PatchAsync($"/api/orders/{order!.Id}/fulfill", null);

        var cancelResponse = await client.PatchAsync($"/api/orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.BadRequest, cancelResponse.StatusCode);
    }

    [Fact]
    public async Task CannotFulfillAnAlreadyCancelledOrder()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var product = await CreateProductAsync(client, "SKU-H", 1000, 10);
        var order = await (await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Jane Customer", new List<CreateOrderItemRequest> { new(product.Id, 2) })))
            .Content.ReadFromJsonAsync<OrderResponse>();
        await client.PatchAsync($"/api/orders/{order!.Id}/cancel", null);

        var fulfillResponse = await client.PatchAsync($"/api/orders/{order.Id}/fulfill", null);

        Assert.Equal(HttpStatusCode.BadRequest, fulfillResponse.StatusCode);
    }

    [Fact]
    public async Task CannotOrderSomeoneElsesProduct()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var product = await CreateProductAsync(client, "SKU-I", 1000, 10);

        client.AuthorizeAs(await client.RegisterAsync());
        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Jane Customer", new List<CreateOrderItemRequest> { new(product.Id, 1) }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OrderingTheSameProductTwiceInOneRequest_SumsTheQuantity()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var product = await CreateProductAsync(client, "SKU-J", 1000, 10);

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Jane Customer", new List<CreateOrderItemRequest>
            {
                new(product.Id, 2),
                new(product.Id, 3),
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/products");
        Assert.Equal(5, products!.Single(p => p.Id == product.Id).QuantityOnHand);
    }
}
