using System.ComponentModel.DataAnnotations;

namespace BasicDotNetPostgresWebsite.Models;

public class Order
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    [Required]
    [StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    [Range(0.01, 100000000)]
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Pending";
}
