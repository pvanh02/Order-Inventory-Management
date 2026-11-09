using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Identity;
using OrderInventory.Application.Orders;
using OrderInventory.Domain.Catalog;
using OrderInventory.Domain.Customers;
using OrderInventory.Domain.Inventory;

namespace OrderInventory.Application.Seeding;

public sealed class OrderSeedService(IOrderData data, ICatalogData catalog, ICustomerData customers, IInventoryWriter writer,
    ISeedHistoryStore history, IUserStore users, OrderService orders, TimeProvider clock)
{
    public const string Sku = "DEMO-ORDER-ITEM";
    public const string SeedUnit = "order-demo";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await data.BeginTransactionAsync(cancellationToken);
        if (await history.ExistsAsync("v1", SeedUnit, cancellationToken)) return;
        if (await catalog.SkuExistsAsync(Sku, cancellationToken))
            throw new InvalidOperationException("The order demo SKU is already reserved; no order seed was applied.");
        var admin = (await users.ListAsync(new UserListQuery { Role = "Admin", IsActive = true, PageSize = 1 }, cancellationToken)).SingleOrDefault()
            ?? throw new InvalidOperationException("An active administrator is required for order seed history.");
        var now = clock.GetUtcNow();
        var category = Category.Create("Demo order items", "Dedicated order demonstration data", now);
        catalog.Add(category);
        await data.SaveChangesAsync(cancellationToken);
        var product = Product.Create(category.Id, Sku, "Demo order item", 10000, null, now);
        catalog.Add(product);
        await data.SaveChangesAsync(cancellationToken);
        catalog.Add(InventoryBalance.Create(product.Id));
        await data.SaveChangesAsync(cancellationToken);
        await writer.ApplyAsync(new(product.Id, 5, MovementType.Receipt, admin.Id, "Demo order opening stock", now), cancellationToken);
        var customer = Customer.Create("Demo order customer", null, null, null, now);
        customers.Add(customer);
        await data.SaveChangesAsync(cancellationToken);
        customers.Add(CustomerAuditEntry.Create(customer.Id, admin.Id, CustomerAuditAction.Created, ["name", "phone", "email", "address"], now));
        await data.SaveChangesAsync(cancellationToken);
        await orders.CreateWithinTransactionAsync(new(admin.Id, admin.Role), new(customer.Id, [new(product.Id, 2)]), cancellationToken);
        history.Add("v1", SeedUnit, now);
        await data.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
