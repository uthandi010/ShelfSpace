using ShelfSpace.App.Core.Models;
using ShelfSpace.App.Core.Services;

namespace ShelfSpace.App.Tests;

public class FakeProductService : IProductService
{
    public List<Product> Products { get; set; } = new();
    public Exception? ThrowOnNextCall { get; set; }

    private void MaybeThrow()
    {
        if (ThrowOnNextCall is { } ex)
        {
            ThrowOnNextCall = null;
            throw ex;
        }
    }

    public Task<List<Product>> ListAsync()
    {
        MaybeThrow();
        return Task.FromResult(Products);
    }

    public Task<Product> CreateAsync(
        string sku, string name, string? description, int priceCents, int quantityOnHand, int? lowStockThreshold)
    {
        MaybeThrow();
        var product = new Product(
            Products.Count + 1, sku, name, description, priceCents, quantityOnHand, lowStockThreshold ?? 5,
            IsLowStock: quantityOnHand <= (lowStockThreshold ?? 5));
        Products.Add(product);
        return Task.FromResult(product);
    }

    public Task<Product> UpdateAsync(int id, string? name, string? description, int? priceCents, int? lowStockThreshold)
    {
        MaybeThrow();
        var existing = Products.First(p => p.Id == id);
        var updated = existing with
        {
            Name = name ?? existing.Name,
            Description = description ?? existing.Description,
            PriceCents = priceCents ?? existing.PriceCents,
            LowStockThreshold = lowStockThreshold ?? existing.LowStockThreshold,
        };
        Products[Products.IndexOf(existing)] = updated;
        return Task.FromResult(updated);
    }

    public Task<Product> RestockAsync(int id, int quantity)
    {
        MaybeThrow();
        var existing = Products.First(p => p.Id == id);
        var updated = existing with { QuantityOnHand = existing.QuantityOnHand + quantity };
        updated = updated with { IsLowStock = updated.QuantityOnHand <= updated.LowStockThreshold };
        Products[Products.IndexOf(existing)] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteAsync(int id)
    {
        MaybeThrow();
        Products.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }
}

public class FakeOrderService : IOrderService
{
    public List<Order> Orders { get; set; } = new();
    public Exception? ThrowOnNextCall { get; set; }
    public List<(string CustomerName, List<(int ProductId, int Quantity)> Items)> CreateCalls { get; } = new();

    private void MaybeThrow()
    {
        if (ThrowOnNextCall is { } ex)
        {
            ThrowOnNextCall = null;
            throw ex;
        }
    }

    public Task<List<Order>> ListAsync()
    {
        MaybeThrow();
        return Task.FromResult(Orders);
    }

    public Task<Order> CreateAsync(string customerName, List<(int ProductId, int Quantity)> items)
    {
        MaybeThrow();
        CreateCalls.Add((customerName, items));
        var order = new Order(Orders.Count + 1, customerName, "Pending", DateTime.UtcNow, 0, new List<OrderItem>());
        Orders.Add(order);
        return Task.FromResult(order);
    }

    public Task<Order> FulfillAsync(int orderId)
    {
        MaybeThrow();
        var existing = Orders.First(o => o.Id == orderId);
        var updated = existing with { Status = "Fulfilled" };
        Orders[Orders.IndexOf(existing)] = updated;
        return Task.FromResult(updated);
    }

    public Task<Order> CancelAsync(int orderId)
    {
        MaybeThrow();
        var existing = Orders.First(o => o.Id == orderId);
        var updated = existing with { Status = "Cancelled" };
        Orders[Orders.IndexOf(existing)] = updated;
        return Task.FromResult(updated);
    }
}

public class FakeAuthService : IAuthService
{
    public bool IsAuthenticated { get; set; }
    public Exception? ThrowOnNextCall { get; set; }
    public (string Email, string Password)? LastLogin { get; private set; }
    public (string Name, string Email, string Password)? LastRegister { get; private set; }

    public Task<AuthResult> LoginAsync(string email, string password)
    {
        LastLogin = (email, password);
        if (ThrowOnNextCall is { } ex)
        {
            ThrowOnNextCall = null;
            throw ex;
        }
        IsAuthenticated = true;
        return Task.FromResult(new AuthResult("token", 1, "Test User", email));
    }

    public Task<AuthResult> RegisterAsync(string name, string email, string password)
    {
        LastRegister = (name, email, password);
        if (ThrowOnNextCall is { } ex)
        {
            ThrowOnNextCall = null;
            throw ex;
        }
        IsAuthenticated = true;
        return Task.FromResult(new AuthResult("token", 1, name, email));
    }

    public void Logout() => IsAuthenticated = false;
}

public class FakeDashboardService : IDashboardService
{
    public DashboardSummary Summary { get; set; } = new(0, 0, 0, 0, 0);
    public Exception? ThrowOnNextCall { get; set; }

    public Task<DashboardSummary> GetSummaryAsync()
    {
        if (ThrowOnNextCall is { } ex)
        {
            ThrowOnNextCall = null;
            throw ex;
        }
        return Task.FromResult(Summary);
    }
}
