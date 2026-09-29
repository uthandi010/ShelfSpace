using ShelfSpace.App.Core.Models;

namespace ShelfSpace.App.Core.Services;

public interface IProductService
{
    Task<List<Product>> ListAsync();
    Task<Product> CreateAsync(string sku, string name, string? description, int priceCents, int quantityOnHand, int? lowStockThreshold);
    Task<Product> UpdateAsync(int id, string? name, string? description, int? priceCents, int? lowStockThreshold);
    Task<Product> RestockAsync(int id, int quantity);
    Task DeleteAsync(int id);
}

public class ProductService : IProductService
{
    private readonly ApiClient _apiClient;

    public ProductService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<List<Product>> ListAsync() => _apiClient.GetAsync<List<Product>>("/api/products");

    public Task<Product> CreateAsync(
        string sku, string name, string? description, int priceCents, int quantityOnHand, int? lowStockThreshold) =>
        _apiClient.PostAsync<Product>(
            "/api/products",
            new { sku, name, description, priceCents, quantityOnHand, lowStockThreshold });

    public Task<Product> UpdateAsync(int id, string? name, string? description, int? priceCents, int? lowStockThreshold) =>
        _apiClient.PatchAsync<Product>($"/api/products/{id}", new { name, description, priceCents, lowStockThreshold });

    public Task<Product> RestockAsync(int id, int quantity) =>
        _apiClient.PostAsync<Product>($"/api/products/{id}/restock", new { quantity });

    public Task DeleteAsync(int id) => _apiClient.DeleteAsync($"/api/products/{id}");
}
