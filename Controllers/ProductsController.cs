using BasicDotNetPostgresWebsite.Data;
using BasicDotNetPostgresWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BasicDotNetPostgresWebsite.Controllers;

[Authorize(Policy = "StoreAdmin")]
[Route("manage/products")]
public class ProductsController(AppDbContext db, IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var products = await db.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .OrderBy(product => product.Name)
            .ToListAsync();

        return View(products);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View(new Product { StockQuantity = 1 });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (product.CategoryId is int categoryId &&
            !await db.Categories.AnyAsync(category => category.Id == categoryId))
        {
            ModelState.AddModelError(nameof(product.CategoryId), "Select a valid category.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync();
            return View(product);
        }

        if (!await SaveImageAsync(product))
        {
            await PopulateCategoriesAsync();
            return View(product);
        }

        db.Products.Add(product);
        await db.SaveChangesAsync();
        TempData["Success"] = "Product created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        await PopulateCategoriesAsync();
        return View(product);
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product input)
    {
        if (id != input.Id)
            return BadRequest();

        var product = await db.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        if (input.CategoryId is int categoryId &&
            !await db.Categories.AnyAsync(category => category.Id == categoryId))
        {
            ModelState.AddModelError(nameof(input.CategoryId), "Select a valid category.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync();
            return View(input);
        }

        product.Name = input.Name.Trim();
        product.Price = input.Price;
        product.CategoryId = input.CategoryId;
        product.Unit = input.Unit.Trim();
        product.StockQuantity = input.StockQuantity;
        product.OriginalPrice = input.OriginalPrice;
        if (!await SaveImageAsync(product, input.Image))
        {
            await PopulateCategoriesAsync();
            return View(input);
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Product updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        TempData["Success"] = "Product deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategoriesAsync()
    {
        ViewBag.Categories = new SelectList(
            await db.Categories.AsNoTracking().OrderBy(category => category.Name).ToListAsync(),
            nameof(Category.Id),
            nameof(Category.Name));
    }

    private Task<bool> SaveImageAsync(Product product) => SaveImageAsync(product, product.Image);

    private async Task<bool> SaveImageAsync(Product product, IFormFile? image)
    {
        if (image is not { Length: > 0 })
            return true;

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };
        var extension = Path.GetExtension(image.FileName);
        if (!allowedExtensions.Contains(extension) || image.Length > 2 * 1024 * 1024)
        {
            ModelState.AddModelError(nameof(product.Image), "Use a JPG, PNG, or WEBP image smaller than 2 MB.");
            return false;
        }

        var uploadsPath = Path.Combine(environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsPath);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(uploadsPath, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await image.CopyToAsync(stream);
        product.ImageUrl = $"/uploads/{fileName}";
        return true;
    }
}