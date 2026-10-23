
using BasicDotNetPostgresWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace BasicDotNetPostgresWebsite.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<StoreUser> StoreUsers => Set<StoreUser>();
    public DbSet<CustomerOrder> CustomerOrders => Set<CustomerOrder>();
    public DbSet<CustomerOrderItem> CustomerOrderItems => Set<CustomerOrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.Name).HasColumnName("name");
            entity.Property(p => p.Price).HasColumnName("price");
            entity.Property(p => p.ImageUrl).HasColumnName("image_url");
            entity.Property(p => p.CategoryId).HasColumnName("category_id");
            entity.Property(p => p.Unit).HasColumnName("unit");
            entity.Property(p => p.StockQuantity).HasColumnName("stock_quantity");
            entity.Property(p => p.OriginalPrice).HasColumnName("original_price");
            entity.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.Property(category => category.Id).HasColumnName("id");
            entity.Property(category => category.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");

            entity.Property(o => o.Id).HasColumnName("id");
            entity.Property(o => o.ProductId).HasColumnName("product_id");
            entity.Property(o => o.CustomerName).HasColumnName("customer_name");
            entity.Property(o => o.Email).HasColumnName("email");
            entity.Property(o => o.Phone).HasColumnName("phone");
            entity.Property(o => o.Address).HasColumnName("address");
            entity.Property(o => o.Quantity).HasColumnName("quantity");
            entity.Property(o => o.TotalAmount).HasColumnName("total_amount");
            entity.Property(o => o.CreatedAt).HasColumnName("created_at");
            entity.Property(o => o.Status).HasColumnName("status");
        });

        modelBuilder.Entity<StoreUser>(entity =>
        {
            entity.ToTable("store_users");

            entity.Property(user => user.Id).HasColumnName("id");
            entity.Property(user => user.Name).HasColumnName("name");
            entity.Property(user => user.Email).HasColumnName("email");
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash");
            entity.Property(user => user.Address).HasColumnName("address");
            entity.Property(user => user.IsAdmin).HasColumnName("is_admin");
            entity.Property(user => user.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<CustomerOrder>(entity =>
        {
            entity.ToTable("customer_orders");
            entity.Property(order => order.Id).HasColumnName("id");
            entity.Property(order => order.UserId).HasColumnName("user_id");
            entity.Property(order => order.CustomerName).HasColumnName("customer_name");
            entity.Property(order => order.Phone).HasColumnName("phone");
            entity.Property(order => order.Email).HasColumnName("email");
            entity.Property(order => order.Address).HasColumnName("address");
            entity.Property(order => order.DeliveryInstructions).HasColumnName("delivery_instructions");
            entity.Property(order => order.Subtotal).HasColumnName("subtotal");
            entity.Property(order => order.TotalAmount).HasColumnName("total_amount");
            entity.Property(order => order.Status).HasColumnName("status");
            entity.Property(order => order.CreatedAt).HasColumnName("created_at");
            entity.HasMany(order => order.Items)
                .WithOne(item => item.Order)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerOrderItem>(entity =>
        {
            entity.ToTable("customer_order_items");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.OrderId).HasColumnName("order_id");
            entity.Property(item => item.ProductId).HasColumnName("product_id");
            entity.Property(item => item.ProductName).HasColumnName("product_name");
            entity.Property(item => item.ImageUrl).HasColumnName("image_url");
            entity.Property(item => item.UnitPrice).HasColumnName("unit_price");
            entity.Property(item => item.Quantity).HasColumnName("quantity");
            entity.Property(item => item.LineTotal).HasColumnName("line_total");
            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

