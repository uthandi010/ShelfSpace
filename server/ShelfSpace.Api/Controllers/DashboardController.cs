using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSpace.Api.Auth;
using ShelfSpace.Api.Data;
using ShelfSpace.Api.Dtos;
using ShelfSpace.Api.Models;

namespace ShelfSpace.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly ShelfSpaceContext _context;

    public DashboardController(ShelfSpaceContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummary>> GetSummary()
    {
        var userId = User.GetUserId();

        var products = await _context.Products.Where(p => p.UserId == userId).ToListAsync();
        var orders = await _context.Orders.Where(o => o.UserId == userId).ToListAsync();

        var totalRevenueCents = await _context.OrderItems
            .Where(oi => oi.Order!.UserId == userId && oi.Order.Status == OrderStatus.Fulfilled)
            .SumAsync(oi => (long)oi.Quantity * oi.UnitPriceCentsAtOrder);

        var summary = new DashboardSummary(
            TotalProducts: products.Count,
            LowStockCount: products.Count(p => p.IsLowStock),
            TotalOrders: orders.Count,
            PendingOrders: orders.Count(o => o.Status == OrderStatus.Pending),
            TotalRevenueCents: totalRevenueCents
        );

        return Ok(summary);
    }
}
