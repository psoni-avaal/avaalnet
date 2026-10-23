using System.ComponentModel.DataAnnotations;

namespace BasicDotNetPostgresWebsite.Models;

public sealed class CustomerOrder
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Required]
    [StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [StringLength(500)]
    public string? DeliveryInstructions { get; set; }

    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CustomerOrderItem> Items { get; set; } = [];
}

public sealed class CustomerOrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int? ProductId { get; set; }

    [Required]
    [StringLength(120)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? ImageUrl { get; set; }

    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    public CustomerOrder? Order { get; set; }
}