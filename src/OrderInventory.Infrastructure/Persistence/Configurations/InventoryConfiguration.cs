using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderInventory.Domain.Catalog;
using OrderInventory.Domain.Identity;
using OrderInventory.Domain.Inventory;

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
            table.HasCheckConstraint("CK_InventoryMovements_Type", "([Type] = 1 AND [Delta] > 0) OR ([Type] = 2 AND LEN(LTRIM(RTRIM([Reason]))) > 0 AND [Reason] IS NOT NULL)");
        });
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Reason).HasMaxLength(512);
        entity.HasOne<Product>().WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UserAccount>().WithMany().HasForeignKey(item => item.ActorId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(item => new { item.ProductId, item.Id });
        entity.HasIndex(item => item.OccurredAt);
        entity.HasIndex(item => item.ActorId);
    }
}
