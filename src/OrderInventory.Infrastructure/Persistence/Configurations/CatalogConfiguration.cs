using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderInventory.Domain.Catalog;

namespace OrderInventory.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> entity)
    {
        entity.ToTable("Categories");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Name).HasMaxLength(120).IsRequired();
        entity.Property(item => item.Description).HasMaxLength(1000);
        entity.Property(item => item.RowVersion).IsRowVersion();
        entity.HasIndex(item => item.IsDeleted);
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.ToTable("Products", table => table.HasCheckConstraint("CK_Products_UnitPrice", "[UnitPrice] > 0"));
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Sku).HasMaxLength(64).UseCollation("Latin1_General_100_CI_AS").IsRequired();
        entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
        entity.Property(item => item.Description).HasMaxLength(2000);
        entity.Property(item => item.UnitPrice).HasPrecision(18, 0);
        entity.Property(item => item.RowVersion).IsRowVersion();
        entity.HasIndex(item => item.Sku).IsUnique();
        entity.HasIndex(item => new { item.CategoryId, item.IsDeleted });
        entity.HasOne<Category>().WithMany().HasForeignKey(item => item.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
