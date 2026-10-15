using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShelfSpace.Api.Models;

public class Product
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? Owner { get; set; }

    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PriceCents { get; set; }
    public int QuantityOnHand { get; set; }
    public int LowStockThreshold { get; set; } = 5;

    /// <summary>
    /// Optimistic concurrency token. SQLite has no rowversion type, so
    /// ShelfSpaceContext assigns a fresh value on every insert/update.
    /// </summary>
    [ConcurrencyCheck]
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public bool IsLowStock => QuantityOnHand <= LowStockThreshold;
}
