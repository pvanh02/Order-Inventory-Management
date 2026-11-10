using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderInventory.Domain.Catalog;
using OrderInventory.Domain.Customers;
using OrderInventory.Domain.Identity;
using OrderInventory.Domain.Orders;

namespace OrderInventory.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_Total", "[Total] > 0");
            table.HasCheckConstraint("CK_Orders_State", "([Status] = 1 AND [CompletedAt] IS NULL AND [CompletedBy] IS NULL AND [CancelledAt] IS NULL) OR ([Status] = 2 AND [CompletedAt] IS NOT NULL AND [CompletedBy] IS NOT NULL AND [CancelledAt] IS NULL)");
        });
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Total).HasPrecision(18, 0);
        entity.Property(item => item.RowVersion).IsRowVersion();
        entity.HasOne<Customer>().WithMany().HasForeignKey(item => item.CustomerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UserAccount>().WithMany().HasForeignKey(item => item.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UserAccount>().WithMany().HasForeignKey(item => item.CompletedBy).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(item => item.Lines).WithOne().HasForeignKey(line => line.OrderId).OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(item => item.Lines).HasField("lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.HasIndex(item => new { item.CreatedAt, item.Id });
    }
}

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> entity)
    {
        entity.ToTable("OrderLines", table =>
        {
            table.HasCheckConstraint("CK_OrderLines_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CK_OrderLines_Amounts", "[UnitPriceSnapshot] > 0 AND [LineTotal] > 0 AND CONVERT(decimal(38,0), [LineTotal]) = CONVERT(decimal(38,0), [UnitPriceSnapshot]) * [Quantity]");
        });
        entity.HasKey(item => item.Id);
        entity.HasAlternateKey(item => new { item.Id, item.ProductId });
        entity.HasIndex(item => new { item.OrderId, item.ProductId }).IsUnique();
        entity.Property(item => item.SkuSnapshot).HasMaxLength(64).IsRequired();
        entity.Property(item => item.NameSnapshot).HasMaxLength(200).IsRequired();
        entity.Property(item => item.UnitPriceSnapshot).HasPrecision(18, 0);
        entity.Property(item => item.LineTotal).HasPrecision(18, 0);
        entity.HasOne<Product>().WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
