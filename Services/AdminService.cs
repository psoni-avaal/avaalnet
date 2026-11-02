using BasicDotNetPostgresWebsite.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Threading;
using BasicDotNetPostgresWebsite.DTOs;
using BasicDotNetPostgresWebsite.Data;
using Microsoft.Extensions.Logging;

namespace BasicDotNetPostgresWebsite.Services;

public interface IAdminService
{
    Task<StoreUser> CreateUserAsync(CreateUserRequest request, CancellationToken ct);
    Task<StoreUserResponse> GetUserByIdAsync(int id, CancellationToken ct);
    Task<Product> CreateProductAsync(CreateProductRequest request, CancellationToken ct);
    Task<ProductResponse> GetProductByIdAsync(int id, CancellationToken ct);
}

public sealed class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(AppDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StoreUser> CreateUserAsync(CreateUserRequest request, CancellationToken ct)
    {
        // In a real app, password hashing must occur here.
        var newUser = new StoreUser
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = request.Password, // WARNING: Plain text password in demo
            Address = request.Address,
            IsAdmin = request.Name.Equals("Admin User", StringComparison.OrdinalIgnoreCase), // Simple admin check for demo
            CreatedAt = DateTimeOffset.UtcNow
        };
        _context.StoreUsers.Add(newUser);
        await _context.SaveChangesAsync(ct);
        return newUser;
    }

    public async Task<StoreUserResponse> GetUserByIdAsync(int id, CancellationToken ct)
    {
        var user = await _context.StoreUsers.FindAsync(id);
        return user is null ? null : new StoreUserResponse(user.Id, user.Name, user.Email, user.IsAdmin, user.CreatedAt);
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request, CancellationToken ct)
    {
        var category = await _context.Categories.FindAsync(request.CategoryId);
        if (category == null)
        {
            throw new KeyNotFoundException($"Category with ID {request.CategoryId} not found.");
        }

        var newProduct = new Product
        {
            Name = request.Name,
            Price = request.Price,
            OriginalPrice = request.OriginalPrice,
            StockQuantity = request.StockQuantity,
            ImageUrl = request.ImageUrl,
            Unit = request.Unit,
            CategoryId = request.CategoryId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _context.Products.Add(newProduct);
        await _context.SaveChangesAsync(ct);
        return newProduct;
    }

    public async Task<ProductResponse> GetProductByIdAsync(int id, CancellationToken ct)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return product is null ? null : new ProductResponse(
            product.Id,
            product.Name,
            product.Price,
            product.Category,
            product.StockQuantity > 0,
            product.CreatedAt
        );
    }
}