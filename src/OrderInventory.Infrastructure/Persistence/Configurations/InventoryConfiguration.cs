using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderInventory.Domain.Catalog;
using OrderInventory.Domain.Identity;
using OrderInventory.Domain.Inventory;
using OrderInventory.Domain.Orders;

namespace OrderInventory.Infrastructure.Persistence.Configurations;

internal sealed class BalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> entity)
    {
        entity.ToTable("InventoryBalances", table => table.HasCheckConstraint("CK_InventoryBalances_Nonnegative", "[OnHand] >= 0"));
        entity.HasKey(item => item.ProductId);
        entity.HasOne<Product>().WithOne().HasForeignKey<InventoryBalance>(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> entity)
    {
        entity.ToTable("InventoryMovements", table =>
        {
            table.HasCheckConstraint("CK_InventoryMovements_Delta", "[Delta] <> 0 AND [Delta] >= -2147483647");
            table.HasCheckConstraint("CK_InventoryMovements_Balance", "[BalanceAfter] >= 0");
            table.HasCheckConstraint("CK_InventoryMovements_Type", "([Type] = 1 AND [Delta] > 0 AND [OrderLineId] IS NULL) OR ([Type] = 2 AND LEN(LTRIM(RTRIM([Reason]))) > 0 AND [Reason] IS NOT NULL AND [OrderLineId] IS NULL) OR ([Type] = 3 AND [Delta] < 0 AND [OrderLineId] IS NOT NULL)");
        });
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Reason).HasMaxLength(512);
        entity.HasOne<Product>().WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UserAccount>().WithMany().HasForeignKey(item => item.ActorId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(item => new { item.ProductId, item.Id });
        entity.HasIndex(item => item.OccurredAt);
        entity.HasIndex(item => item.ActorId);
        entity.HasOne<OrderLine>().WithMany().HasForeignKey(item => new { item.OrderLineId, item.ProductId })
            .HasPrincipalKey(item => new { item.Id, item.ProductId }).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(item => item.OrderLineId).IsUnique().HasFilter("[OrderLineId] IS NOT NULL");
    }
}
