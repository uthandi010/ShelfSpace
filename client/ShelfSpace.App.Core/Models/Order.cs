namespace ShelfSpace.App.Core.Models;

public record OrderItem(int ProductId, string ProductName, int Quantity, int UnitPriceCentsAtOrder);

public record Order(
    int Id,
    string CustomerName,
    string Status,
    DateTime CreatedAtUtc,
    long TotalCents,
    List<OrderItem> Items
)
{
    public decimal Total => TotalCents / 100m;
}
