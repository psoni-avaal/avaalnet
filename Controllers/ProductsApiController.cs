
using BasicDotNetPostgresWebsite.Data;
using BasicDotNetPostgresWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BasicDotNetPostgresWebsite.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsApiController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await db.Products.FindAsync(id);

        if (product == null)
            return NotFound(new { message = "Product not found" });

        return Ok(product);
    }

    [HttpPost]
    [Authorize(Policy = "StoreAdmin")]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        product.Id = 0;

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "StoreAdmin")]
    public async Task<IActionResult> Update(int id, Product input)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var product = await db.Products.FindAsync(id);

        if (product == null)
            return NotFound(new { message = "Product not found" });

        product.Name = input.Name;
        product.Price = input.Price;
        product.CategoryId = input.CategoryId;
        product.Unit = input.Unit;
        product.StockQuantity = input.StockQuantity;
        product.OriginalPrice = input.OriginalPrice;

        await db.SaveChangesAsync();

        return Ok(product);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "StoreAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);

        if (product == null)
            return NotFound(new { message = "Product not found" });

        db.Products.Remove(product);
        await db.SaveChangesAsync();

        return NoContent();
    }
}

