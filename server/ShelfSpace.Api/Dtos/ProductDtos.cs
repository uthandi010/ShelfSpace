namespace ShelfSpace.Api.Dtos;

public record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    int PriceCents,
    int QuantityOnHand,
    int? LowStockThreshold
);

public record UpdateProductRequest(
    string? Name,
    string? Description,
    int? PriceCents,
    int? LowStockThreshold
);

public record RestockRequest(int Quantity);

public record ProductResponse(
    int Id,
    string Sku,
    string Name,
    string? Description,
    int PriceCents,
    int QuantityOnHand,
    int LowStockThreshold,
    bool IsLowStock
);
