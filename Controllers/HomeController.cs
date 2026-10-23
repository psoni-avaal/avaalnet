
using System.Data.Common;
using BasicDotNetPostgresWebsite.Data;
using BasicDotNetPostgresWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BasicDotNetPostgresWebsite.Controllers;

public class HomeController(AppDbContext db, ILogger<HomeController> logger) : Controller
{
    private static readonly string[] GroceryCategories =
    [
        "Fruits & vegetables",
        "Dairy & bakery",
        "Snacks & beverages",
        "Personal care",
        "Household essentials",
        "Pantry staples"
    ];

    public async Task<IActionResult> Index(string? search, string? category)
    {
        try
        {
            var categories = await db.Categories
                .AsNoTracking()
                .Where(item => GroceryCategories.Contains(item.Name))
                .OrderBy(item => item.Name)
                .ToListAsync();
            var categoryIds = categories.Select(item => item.Id).ToArray();
            var query = db.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Where(product => product.CategoryId != null && categoryIds.Contains(product.CategoryId.Value));

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(product => product.Category != null && product.Category.Name == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchPattern = $"%{EscapeLikePattern(search.Trim())}%";
                query = query.Where(product => EF.Functions.ILike(product.Name, searchPattern) ||
                                               (product.Category != null && EF.Functions.ILike(product.Category.Name, searchPattern)));
            }

            var products = await query
                .OrderBy(product => product.Name)
                .ToListAsync();
            var groceryProducts = string.IsNullOrWhiteSpace(search)
                ? products
                : products.Where(product => product.CategoryId != null && categoryIds.Contains(product.CategoryId.Value)).ToList();

            var legacyBestSellerIds = await db.Orders
                .AsNoTracking()
                .GroupBy(order => order.ProductId)
                .OrderByDescending(group => group.Sum(order => order.Quantity))
                .Select(group => group.Key)
                .Take(8)
                .ToListAsync();
            var groceryBestSellerIds = await db.CustomerOrderItems
                .AsNoTracking()
                .Where(item => item.ProductId != null)
                .GroupBy(item => item.ProductId!.Value)
                .OrderByDescending(group => group.Sum(item => item.Quantity))
                .Select(group => group.Key)
                .Take(8)
                .ToListAsync();
            var bestSellerIds = legacyBestSellerIds.Concat(groceryBestSellerIds).Distinct().ToArray();
            var bestSellers = await db.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Where(product => bestSellerIds.Contains(product.Id) &&
                                  product.CategoryId != null && categoryIds.Contains(product.CategoryId.Value))
                .ToListAsync();

            return View(new StorefrontViewModel
            {
                Search = search?.Trim() ?? string.Empty,
                Category = category ?? string.Empty,
                Products = products,
                Categories = categories.Select(item => item.Name).ToList(),
                FeaturedProducts = groceryProducts.Where(product => product.StockQuantity > 0).Take(4).ToList(),
                BestSellers = bestSellers,
                Shelves = categories.Select(item => new CategoryShelfViewModel
                {
                    Name = item.Name,
                    Products = groceryProducts
                        .Where(product => product.CategoryId == item.Id && product.StockQuantity > 0)
                        .Take(6)
                        .ToList()
                }).Where(shelf => shelf.Products.Count > 0).ToList()
            });
        }
        catch (DbException exception)
        {
            logger.LogError(exception, "Could not load grocery products from PostgreSQL.");
            return View(new StorefrontViewModel
            {
                Search = search?.Trim() ?? string.Empty,
                Category = category ?? string.Empty,
                DatabaseError = "The catalog is temporarily unavailable. Please try again shortly."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == id);

        return product == null ? NotFound() : View(product);
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}

