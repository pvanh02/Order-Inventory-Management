namespace OrderInventory.Application.Catalog;

public sealed record CategoryDto(int Id, string Name, string? Description, DateTimeOffset CreatedAt);
public sealed record ProductDto(int Id, int CategoryId, string Sku, string Name, string? Description, decimal UnitPrice, int OnHand, DateTimeOffset CreatedAt);
public sealed record CategoryRequest(string Name, string? Description = null);
public sealed record CreateProductRequest(int CategoryId, string Sku, string Name, decimal UnitPrice, string? Description = null);
public sealed record UpdateProductRequest(int CategoryId, string Name, decimal UnitPrice, string? Description = null, string? Sku = null);

public sealed record CategoryListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string Sort { get; init; } = "id";
}

public sealed record ProductListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? InStock { get; init; }
    public string Sort { get; init; } = "id";
}
