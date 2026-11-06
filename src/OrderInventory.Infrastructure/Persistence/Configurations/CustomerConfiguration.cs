using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderInventory.Domain.Customers;
using OrderInventory.Domain.Identity;

namespace OrderInventory.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> entity)
    {
        entity.ToTable("Customers");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Name).HasMaxLength(CustomerRules.MaximumNameLength).IsRequired();
        entity.Property(item => item.Phone).HasMaxLength(CustomerRules.MaximumPhoneLength);
        entity.Property(item => item.Email).HasMaxLength(CustomerRules.MaximumEmailLength);
        entity.Property(item => item.Address).HasMaxLength(CustomerRules.MaximumAddressLength);
        entity.Property(item => item.RowVersion).IsRowVersion();
        entity.HasIndex(item => new { item.IsDeleted, item.Name, item.Id });
    }
}

internal sealed class CustomerAuditEntryConfiguration : IEntityTypeConfiguration<CustomerAuditEntry>
{
    public void Configure(EntityTypeBuilder<CustomerAuditEntry> entity)
    {
        entity.ToTable("CustomerAuditEntries", table => table.HasCheckConstraint("CK_CustomerAuditEntries_Action", "[Action] IN (1, 2, 3)"));
        entity.HasKey(item => item.Id);
        entity.Property(item => item.ChangedFields).HasMaxLength(64).IsRequired();
        entity.HasIndex(item => new { item.CustomerId, item.OccurredAt, item.Id });
        entity.HasOne<Customer>().WithMany().HasForeignKey(item => item.CustomerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UserAccount>().WithMany().HasForeignKey(item => item.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
