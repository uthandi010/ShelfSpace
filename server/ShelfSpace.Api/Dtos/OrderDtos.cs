namespace ShelfSpace.Api.Dtos;

public record CreateOrderItemRequest(int ProductId, int Quantity);
public record CreateOrderRequest(string CustomerName, List<CreateOrderItemRequest> Items);

public record OrderItemResponse(int ProductId, string ProductName, int Quantity, int UnitPriceCentsAtOrder);

public record OrderResponse(
    int Id,
    string CustomerName,
    string Status,
    DateTime CreatedAtUtc,
    long TotalCents,
    List<OrderItemResponse> Items
);

public record DashboardSummary(
    int TotalProducts,
    int LowStockCount,
    int TotalOrders,
    int PendingOrders,
    long TotalRevenueCents
);
