namespace ShelfSpace.App.Core.Models;

public record DashboardSummary(
    int TotalProducts,
    int LowStockCount,
    int TotalOrders,
    int PendingOrders,
    long TotalRevenueCents
)
{
    public decimal TotalRevenue => TotalRevenueCents / 100m;
}
