using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Identity;
using OrderInventory.Domain.Catalog;
using OrderInventory.Domain.Inventory;

namespace OrderInventory.Application.Seeding;

public sealed class CatalogSeedService(ICatalogData catalog, IInventoryWriter writer, ISeedHistoryStore history, IUserStore users, TimeProvider clock)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await catalog.BeginTransactionAsync(cancellationToken);
        if (await history.ExistsAsync("v1", "catalog-inventory", cancellationToken)) return;
        if (await catalog.SkuExistsAsync("DEMO-COFFEE", cancellationToken) || await catalog.SkuExistsAsync("DEMO-TEA", cancellationToken))
            throw new InvalidOperationException("Demo SKUs conflict with existing products; no catalog seed was applied.");
        var admin = (await users.ListAsync(new UserListQuery { Role = "Admin", IsActive = true, PageSize = 1 }, cancellationToken)).SingleOrDefault()
            ?? throw new InvalidOperationException("An active administrator is required for catalog seed history.");
        var now = clock.GetUtcNow();
        var category = Category.Create("Demo beverages", "Local demonstration data", now);
        catalog.Add(category);
        await catalog.SaveChangesAsync(cancellationToken);
        foreach (var sample in new[] { (Sku: "DEMO-COFFEE", Name: "Demo coffee", Price: 25000m, Quantity: 10), (Sku: "DEMO-TEA", Name: "Demo tea", Price: 18000m, Quantity: 20) })
        {
            var product = Product.Create(category.Id, sample.Sku, sample.Name, sample.Price, null, now);
            catalog.Add(product);
            await catalog.SaveChangesAsync(cancellationToken);
            catalog.Add(InventoryBalance.Create(product.Id));
            await catalog.SaveChangesAsync(cancellationToken);
            await writer.ApplyAsync(new(product.Id, sample.Quantity, MovementType.Receipt, admin.Id, "Demo opening stock", now), cancellationToken);
            await catalog.SaveChangesAsync(cancellationToken);
        }
        history.Add("v1", "catalog-inventory", now);
        await catalog.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
