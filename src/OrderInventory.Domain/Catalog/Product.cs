namespace OrderInventory.Domain.Catalog;

public sealed class Product
{
    private Product() { }
    public int Id { get; set; }
    public int CategoryId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal UnitPrice { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public byte[] RowVersion { get; set; } = [];

    public static Product Create(int categoryId, string sku, string name, decimal unitPrice, string? description, DateTimeOffset createdAt)
    {
        var product = new Product { Sku = CatalogRules.Sku(sku), CreatedAt = createdAt };
        product.Update(categoryId, name, unitPrice, description);
        return product;
    }

    public void Update(int categoryId, string name, decimal unitPrice, string? description)
    {
        if (categoryId <= 0) throw new ArgumentOutOfRangeException(nameof(categoryId));
        CategoryId = categoryId;
        Name = CatalogRules.Name(name, 200);
        UnitPrice = CatalogRules.Price(unitPrice);
        Description = CatalogRules.Description(description, 2000);
    }

    public void Delete() => IsDeleted = true;
}
