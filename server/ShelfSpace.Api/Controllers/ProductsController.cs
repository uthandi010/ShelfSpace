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
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ShelfSpaceContext _context;

    public ProductsController(ShelfSpaceContext context)
    {
        _context = context;
    }

    private static ProductResponse ToResponse(Product p) => new(
        p.Id, p.Sku, p.Name, p.Description, p.PriceCents, p.QuantityOnHand, p.LowStockThreshold, p.IsLowStock
    );

    [HttpGet]
    public async Task<ActionResult<List<ProductResponse>>> GetProducts()
    {
        var userId = User.GetUserId();
        var products = await _context.Products
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Ok(products.Select(ToResponse));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> CreateProduct(CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "SKU and name are required." });
        }
        if (request.PriceCents < 0)
        {
            return BadRequest(new { message = "Price cannot be negative." });
        }
        if (request.QuantityOnHand < 0)
        {
            return BadRequest(new { message = "Quantity cannot be negative." });
        }

        var userId = User.GetUserId();
        var sku = request.Sku.Trim();

        var skuTaken = await _context.Products.AnyAsync(p => p.UserId == userId && p.Sku == sku);
        if (skuTaken)
        {
            return Conflict(new { message = "You already have a product with that SKU." });
        }

        var product = new Product
        {
            UserId = userId,
            Sku = sku,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            PriceCents = request.PriceCents,
            QuantityOnHand = request.QuantityOnHand,
            LowStockThreshold = request.LowStockThreshold ?? 5,
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return Ok(ToResponse(product));
    }

    [HttpPatch("{productId:int}")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(int productId, UpdateProductRequest request)
    {
        var userId = User.GetUserId();
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId);
        if (product is null)
        {
            return NotFound();
        }

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Name cannot be blank." });
            }
            product.Name = request.Name.Trim();
        }
        if (request.Description is not null)
        {
            product.Description = request.Description.Trim();
        }
        if (request.PriceCents is int price)
        {
            if (price < 0)
            {
                return BadRequest(new { message = "Price cannot be negative." });
            }
            product.PriceCents = price;
        }
        if (request.LowStockThreshold is int threshold)
        {
            if (threshold < 0)
            {
                return BadRequest(new { message = "Low stock threshold cannot be negative." });
            }
            product.LowStockThreshold = threshold;
        }

        await _context.SaveChangesAsync();
        return Ok(ToResponse(product));
    }

    [HttpPost("{productId:int}/restock")]
    public async Task<ActionResult<ProductResponse>> Restock(int productId, RestockRequest request)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest(new { message = "Restock quantity must be greater than zero." });
        }

        var userId = User.GetUserId();
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId);
        if (product is null)
        {
            return NotFound();
        }

        product.QuantityOnHand += request.Quantity;
        await _context.SaveChangesAsync();

        return Ok(ToResponse(product));
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> DeleteProduct(int productId)
    {
        var userId = User.GetUserId();
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId);
        if (product is null)
        {
            return NotFound();
        }

        var isUsedInAnOrder = await _context.OrderItems.AnyAsync(oi => oi.ProductId == productId);
        if (isUsedInAnOrder)
        {
            return BadRequest(new { message = "Can't delete a product that appears in an existing order." });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
