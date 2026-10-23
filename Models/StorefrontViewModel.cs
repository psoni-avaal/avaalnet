namespace BasicDotNetPostgresWebsite.Models;

public sealed class StorefrontViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public List<Product> FeaturedProducts { get; set; } = [];
    public List<CategoryShelfViewModel> Shelves { get; set; } = [];
    public List<Product> BestSellers { get; set; } = [];
    public string? DatabaseError { get; set; }
}

public sealed class CategoryShelfViewModel
{
    public string Name { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = [];
}