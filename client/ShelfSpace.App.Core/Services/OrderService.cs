using ShelfSpace.App.Core.Models;

namespace ShelfSpace.App.Core.Services;

public interface IOrderService
{
    Task<List<Order>> ListAsync();
    Task<Order> CreateAsync(string customerName, List<(int ProductId, int Quantity)> items);
    Task<Order> FulfillAsync(int orderId);
    Task<Order> CancelAsync(int orderId);
}

public class OrderService : IOrderService
{
    private readonly ApiClient _apiClient;

    public OrderService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<List<Order>> ListAsync() => _apiClient.GetAsync<List<Order>>("/api/orders");

    public Task<Order> CreateAsync(string customerName, List<(int ProductId, int Quantity)> items)
    {
        var payload = new
        {
            customerName,
            items = items.Select(i => new { productId = i.ProductId, quantity = i.Quantity }).ToList(),
        };
        return _apiClient.PostAsync<Order>("/api/orders", payload);
    }

    public Task<Order> FulfillAsync(int orderId) => _apiClient.PatchAsync<Order>($"/api/orders/{orderId}/fulfill");

    public Task<Order> CancelAsync(int orderId) => _apiClient.PatchAsync<Order>($"/api/orders/{orderId}/cancel");
}
