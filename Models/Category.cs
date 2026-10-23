using System.ComponentModel.DataAnnotations;

namespace BasicDotNetPostgresWebsite.Models;

public sealed class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}