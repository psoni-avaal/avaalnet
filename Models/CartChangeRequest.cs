using System.ComponentModel.DataAnnotations;

namespace BasicDotNetPostgresWebsite.Models;

public sealed class CartChangeRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;
}