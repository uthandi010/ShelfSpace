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
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly ShelfSpaceContext _context;

    public OrdersController(ShelfSpaceContext context)
    {
        _context = context;
    }

    private static OrderResponse ToResponse(Order order) => new(
        order.Id,
        order.CustomerName,
        order.Status.ToString(),
        order.CreatedAtUtc,
        order.Items.Sum(i => (long)i.Quantity * i.UnitPriceCentsAtOrder),
        order.Items.Select(i => new OrderItemResponse(
            i.ProductId, i.Product?.Name ?? "(deleted product)", i.Quantity, i.UnitPriceCentsAtOrder
        )).ToList()
    );

    [HttpGet]
    public async Task<ActionResult<List<OrderResponse>>> GetOrders()
    {
        var userId = User.GetUserId();
        var orders = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync();

        return Ok(orders.Select(ToResponse));
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder(CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return BadRequest(new { message = "Customer name is required." });
        }
        if (request.Items is null || request.Items.Count == 0)
        {
            return BadRequest(new { message = "An order needs at least one item." });
        }

        var userId = User.GetUserId();

        // Sum up requested quantity per product in case the same product
        // was listed more than once in the request.
        var requestedByProduct = new Dictionary<int, int>();
        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                return BadRequest(new { message = "Item quantities must be greater than zero." });
            }
            requestedByProduct[item.ProductId] = requestedByProduct.GetValueOrDefault(item.ProductId) + item.Quantity;
        }

        var productIds = requestedByProduct.Keys.ToList();
        var products = await _context.Products
            .Where(p => p.UserId == userId && productIds.Contains(p.Id))
            .ToListAsync();

        if (products.Count != productIds.Count)
        {
            return BadRequest(new { message = "One or more products were not found." });
        }

        // Check every line has enough stock *before* changing anything, so
        // a partially-out-of-stock order is rejected as a whole rather than
        // partially applied.
        var insufficient = products.Where(p => p.QuantityOnHand < requestedByProduct[p.Id]).ToList();
        if (insufficient.Count > 0)
        {
            var names = string.Join(", ", insufficient.Select(p => p.Name));
            return BadRequest(new { message = $"Not enough stock for: {names}." });
        }

        var order = new Order { UserId = userId, CustomerName = request.CustomerName.Trim() };

        // SQLite serializes writes at the connection/file level, which
        // makes this safe for this project's scale; a multi-writer database
        // in production would additionally want a row-level lock or an
        // optimistic concurrency token on Product.QuantityOnHand.
        using var transaction = await _context.Database.BeginTransactionAsync();

        foreach (var product in products)
        {
            var quantity = requestedByProduct[product.Id];
            product.QuantityOnHand -= quantity;
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = quantity,
                UnitPriceCentsAtOrder = product.PriceCents,
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        await _context.Entry(order).Collection(o => o.Items).Query().Include(i => i.Product).LoadAsync();
        return Ok(ToResponse(order));
    }

    [HttpPatch("{orderId:int}/fulfill")]
    public async Task<ActionResult<OrderResponse>> Fulfill(int orderId)
    {
        var userId = User.GetUserId();
        var order = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Pending)
        {
            return BadRequest(new { message = $"Only a pending order can be fulfilled (this one is {order.Status})." });
        }

        order.Status = OrderStatus.Fulfilled;
        await _context.SaveChangesAsync();
        return Ok(ToResponse(order));
    }

    [HttpPatch("{orderId:int}/cancel")]
    public async Task<ActionResult<OrderResponse>> Cancel(int orderId)
    {
        var userId = User.GetUserId();
        var order = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Pending)
        {
            return BadRequest(new { message = $"Only a pending order can be cancelled (this one is {order.Status})." });
        }

        // Give every reserved unit back to inventory.
        foreach (var item in order.Items)
        {
            if (item.Product is not null)
            {
                item.Product.QuantityOnHand += item.Quantity;
            }
        }

        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();
        return Ok(ToResponse(order));
    }
}
