using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace BasicDotNetPostgresWebsite.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 99999999.99)]
    public decimal Price { get; set; }

    [StringLength(255)]
    public string? ImageUrl { get; set; }

    [Required]
    [StringLength(40)]
    public string Unit { get; set; } = "1 item";

    [Range(0, 100000)]
    public int StockQuantity { get; set; }

    [Range(0.01, 99999999.99)]
    public decimal? OriginalPrice { get; set; }

    public int? CategoryId { get; set; }

    public Category? Category { get; set; }

    [NotMapped]
    public IFormFile? Image { get; set; }
}
