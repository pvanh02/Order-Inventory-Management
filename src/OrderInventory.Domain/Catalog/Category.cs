namespace OrderInventory.Domain.Catalog;

public sealed class Category
{
    private Category() { }
    public int Id { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public byte[] RowVersion { get; set; } = [];

    public static Category Create(string name, string? description, DateTimeOffset createdAt) => new()
    {
        Name = CatalogRules.Name(name, 120), Description = CatalogRules.Description(description, 1000), CreatedAt = createdAt
    };

    public void Update(string name, string? description)
    {
        Name = CatalogRules.Name(name, 120);
        Description = CatalogRules.Description(description, 1000);
    }

    public void Delete() => IsDeleted = true;
}
