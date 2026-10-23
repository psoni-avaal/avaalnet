using System.Security.Claims;
using System.Text.Json;
using BasicDotNetPostgresWebsite.Data;
using BasicDotNetPostgresWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BasicDotNetPostgresWebsite.Controllers;

public class CartController(AppDbContext db) : Controller
{
    private const string SessionCartKey = "quickbasket-cart-v1";

    [HttpGet]
    public async Task<IActionResult> Summary()
    {
        var cart = ReadCart();
        if (cart.Count == 0)
            return Ok(new { items = Array.Empty<object>(), itemCount = 0, subtotal = 0m });

        var products = await db.Products
            .AsNoTracking()
            .Where(product => cart.Keys.Contains(product.Id))
            .ToListAsync();
        var availableIds = products.Select(product => product.Id).ToHashSet();
        foreach (var removedId in cart.Keys.Where(id => !availableIds.Contains(id)).ToList())
            cart.Remove(removedId);
        WriteCart(cart);

        var items = products
            .Where(product => cart.ContainsKey(product.Id))
            .Select(product => new
            {
                productId = product.Id,
                product.Name,
                product.ImageUrl,
                product.Unit,
                product.Price,
                product.StockQuantity,
                quantity = Math.Min(cart[product.Id], product.StockQuantity),
                lineTotal = product.Price * Math.Min(cart[product.Id], product.StockQuantity)
            })
            .Where(item => item.StockQuantity > 0)
            .ToList();

        var itemCount = items.Sum(item => item.quantity);
        var subtotal = items.Sum(item => item.lineTotal);
        return Ok(new { items, itemCount, subtotal });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromBody] CartChangeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid product or quantity." });

        var product = await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.ProductId);
        if (product == null)
            return NotFound(new { message = "This grocery is no longer available." });
        if (product.StockQuantity == 0)
            return Conflict(new { message = "This product is currently sold out." });

        var cart = ReadCart();
        var quantity = cart.GetValueOrDefault(product.Id) + request.Quantity;
        if (quantity > product.StockQuantity)
            return Conflict(new { message = $"Only {product.StockQuantity} available." });

        cart[product.Id] = quantity;
        WriteCart(cart);
        return await Summary();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetQuantity([FromBody] CartChangeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid product or quantity." });

        var product = await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.ProductId);
        if (product == null)
            return NotFound(new { message = "This grocery is no longer available." });
        if (request.Quantity > product.StockQuantity)
            return Conflict(new { message = $"Only {product.StockQuantity} available." });

        var cart = ReadCart();
        if (!cart.ContainsKey(product.Id))
            return NotFound(new { message = "This item is not in your cart." });

        cart[product.Id] = request.Quantity;
        WriteCart(cart);
        return await Summary();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove([FromBody] CartChangeRequest request)
    {
        var cart = ReadCart();
        cart.Remove(request.ProductId);
        WriteCart(cart);
        return await Summary();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var quantities = ReadCart();
        if (quantities.Count == 0)
        {
            return RedirectToAction("Index", "Home");
        }

        var products = await db.Products
            .AsNoTracking()
            .Where(product => quantities.Keys.Contains(product.Id))
            .ToListAsync();

        if (products.Count != quantities.Count || products.Any(product => product.StockQuantity < quantities[product.Id]))
        {
            return RedirectToAction("Index", "Home");
        }

        return View(CreateViewModel(products, quantities));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CartCheckoutViewModel model)
    {
        var quantities = ReadCart();
        if (quantities.Count is 0 or > 30)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            model.Items = await LoadCartItemsAsync(quantities);
            return View(model);
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var products = await db.Products.AsNoTracking()
            .Where(product => quantities.Keys.Contains(product.Id))
            .ToListAsync();
        if (products.Count != quantities.Count)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "A product in your cart is no longer available.";
            return RedirectToAction("Index", "Home");
        }

        foreach (var item in products)
        {
            var quantity = quantities[item.Id];
            var updated = await db.Products
                .Where(product => product.Id == item.Id && product.StockQuantity >= quantity)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(product => product.StockQuantity, product => product.StockQuantity - quantity));

            if (updated != 1)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, $"{item.Name} no longer has enough stock. Please update your cart.");
                model.Items = await LoadCartItemsAsync(quantities);
                return View(model);
            }
        }

        var items = products.Select(product => new CustomerOrderItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ImageUrl = product.ImageUrl,
            UnitPrice = product.Price,
            Quantity = quantities[product.Id],
            LineTotal = product.Price * quantities[product.Id]
        }).ToList();
        var subtotal = items.Sum(item => item.LineTotal);
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Challenge();

        var order = new CustomerOrder
        {
            UserId = userId,
            CustomerName = model.CustomerName.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            Phone = model.Phone.Trim(),
            Address = model.Address.Trim(),
            DeliveryInstructions = model.DeliveryInstructions?.Trim(),
            Subtotal = subtotal,
            TotalAmount = subtotal,
            CreatedAt = DateTime.UtcNow,
            Status = "Pending",
            Items = items
        };

        db.CustomerOrders.Add(order);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        HttpContext.Session.Remove(SessionCartKey);
        TempData["QuickBasketOrderId"] = order.Id.ToString();
        return RedirectToAction(nameof(Confirmation));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Confirmation()
    {
        if (!int.TryParse(TempData["QuickBasketOrderId"]?.ToString(), out var orderId) ||
            !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return RedirectToAction("Index", "Home");

        var order = await db.CustomerOrders
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == orderId && item.UserId == userId);

        return order == null ? NotFound() : View(order);
    }

    private async Task<List<CartLineViewModel>> LoadCartItemsAsync(IReadOnlyDictionary<int, int> quantities)
    {
        var products = await db.Products.AsNoTracking()
            .Where(product => quantities.Keys.Contains(product.Id))
            .ToListAsync();

        return products.Select(product => new CartLineViewModel
        {
            ProductId = product.Id,
            Name = product.Name,
            ImageUrl = product.ImageUrl,
            UnitPrice = product.Price,
            Quantity = quantities[product.Id]
        }).ToList();
    }

    private CartCheckoutViewModel CreateViewModel(
        IEnumerable<Product> products,
        IReadOnlyDictionary<int, int> quantities)
    {
        var lines = products.Select(product => new CartLineViewModel
        {
            ProductId = product.Id,
            Name = product.Name,
            ImageUrl = product.ImageUrl,
            UnitPrice = product.Price,
            Quantity = quantities[product.Id]
        }).ToList();

        return new CartCheckoutViewModel
        {
            Items = lines,
            CustomerName = User.Identity?.Name ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            Address = User.FindFirstValue("address") ?? string.Empty
        };
    }

    private Dictionary<int, int> ReadCart()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, int>>(
                HttpContext.Session.GetString(SessionCartKey) ?? "{}") ?? [];
        }
        catch (JsonException)
        {
            HttpContext.Session.Remove(SessionCartKey);
            return [];
        }
    }

    private void WriteCart(Dictionary<int, int> cart)
    {
        HttpContext.Session.SetString(SessionCartKey, JsonSerializer.Serialize(cart));
    }
}