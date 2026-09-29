using System.Net;
using System.Net.Http.Json;
using ShelfSpace.Api.Dtos;
using Xunit;

namespace ShelfSpace.Api.Tests;

public class ProductsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreateProductRequest MakeProduct(string sku = "SKU-1", int quantity = 10, int? threshold = null) =>
        new(sku, "Widget", "A basic widget", 1500, quantity, threshold);

    [Fact]
    public async Task CreateProduct_ReturnsProductWithComputedLowStockFlag()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());

        var response = await client.PostAsJsonAsync("/api/products", MakeProduct(quantity: 2, threshold: 5));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.True(product!.IsLowStock);
    }

    [Fact]
    public async Task CreateProduct_AboveThreshold_IsNotLowStock()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());

        var response = await client.PostAsJsonAsync("/api/products", MakeProduct(quantity: 50, threshold: 5));

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.False(product!.IsLowStock);
    }

    [Fact]
    public async Task CreateProduct_RejectsDuplicateSkuForSameUser()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        await client.PostAsJsonAsync("/api/products", MakeProduct(sku: "DUP-1"));

        var response = await client.PostAsJsonAsync("/api/products", MakeProduct(sku: "DUP-1"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_AllowsSameSkuForDifferentUsers()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        await client.PostAsJsonAsync("/api/products", MakeProduct(sku: "SHARED-SKU"));

        client.AuthorizeAs(await client.RegisterAsync());
        var response = await client.PostAsJsonAsync("/api/products", MakeProduct(sku: "SHARED-SKU"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_RejectsNegativePrice()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());

        var response = await client.PostAsJsonAsync(
            "/api/products", new CreateProductRequest("SKU-NEG", "Widget", null, -100, 10, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListProducts_OnlyReturnsMyOwnProducts()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        await client.PostAsJsonAsync("/api/products", MakeProduct(sku: "MINE"));

        client.AuthorizeAs(await client.RegisterAsync());
        var response = await client.GetAsync("/api/products");

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.Empty(products!);
    }

    [Fact]
    public async Task Restock_IncreasesQuantity()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var created = await (await client.PostAsJsonAsync("/api/products", MakeProduct(quantity: 5)))
            .Content.ReadFromJsonAsync<ProductResponse>();

        var response = await client.PostAsJsonAsync($"/api/products/{created!.Id}/restock", new RestockRequest(20));

        var updated = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(25, updated!.QuantityOnHand);
    }

    [Fact]
    public async Task Restock_RejectsZeroOrNegativeQuantity()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var created = await (await client.PostAsJsonAsync("/api/products", MakeProduct()))
            .Content.ReadFromJsonAsync<ProductResponse>();

        var response = await client.PostAsJsonAsync($"/api/products/{created!.Id}/restock", new RestockRequest(0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CannotUpdateOrRestockSomeoneElsesProduct()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var created = await (await client.PostAsJsonAsync("/api/products", MakeProduct()))
            .Content.ReadFromJsonAsync<ProductResponse>();

        client.AuthorizeAs(await client.RegisterAsync());
        var updateResponse = await client.PatchAsJsonAsync(
            $"/api/products/{created!.Id}", new UpdateProductRequest("Hijacked", null, null, null));
        var restockResponse = await client.PostAsJsonAsync(
            $"/api/products/{created.Id}/restock", new RestockRequest(10));

        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, restockResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_RemovesIt()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var created = await (await client.PostAsJsonAsync("/api/products", MakeProduct()))
            .Content.ReadFromJsonAsync<ProductResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/products/{created!.Id}");
        var listResponse = await client.GetFromJsonAsync<List<ProductResponse>>("/api/products");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Empty(listResponse!);
    }
}
