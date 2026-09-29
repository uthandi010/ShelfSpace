namespace ShelfSpace.App.Core.Models;

public record Product(
    int Id,
    string Sku,
    string Name,
    string? Description,
    int PriceCents,
    int QuantityOnHand,
    int LowStockThreshold,
    bool IsLowStock
)
{
    public decimal Price => PriceCents / 100m;
}
