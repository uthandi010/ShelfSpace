namespace ShelfSpace.Api.Models;

public enum OrderStatus
{
    Pending = 0,
    Fulfilled = 1,
    Cancelled = 2,
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? Owner { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    /// <summary>
    /// A snapshot of the product's price at the moment the order was
    /// placed, so a later price change never rewrites historical orders.
    /// </summary>
    public int UnitPriceCentsAtOrder { get; set; }
}
