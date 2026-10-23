using System.ComponentModel.DataAnnotations;

namespace BasicDotNetPostgresWebsite.Models;

public sealed class CartCheckoutViewModel
{
    public List<CartLineViewModel> Items { get; set; } = [];

    [Required]
    [StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [StringLength(500)]
    public string? DeliveryInstructions { get; set; }

    public decimal TotalAmount => Items.Sum(item => item.UnitPrice * item.Quantity);
}

public sealed class CartLineViewModel
{
    public int ProductId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
}