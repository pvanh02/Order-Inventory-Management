using Microsoft.EntityFrameworkCore;
using OrderInventory.Domain.Identity;
using OrderInventory.Domain.Catalog;
using OrderInventory.Domain.Inventory;
using OrderInventory.Domain.Customers;
using OrderInventory.Domain.Orders;

namespace OrderInventory.Infrastructure.Persistence;

public sealed class OrderInventoryDbContext(DbContextOptions<OrderInventoryDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<SeedHistory> SeedHistory => Set<SeedHistory>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAuditEntry> CustomerAuditEntries => Set<CustomerAuditEntry>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var users = modelBuilder.Entity<UserAccount>();
        users.ToTable("UserAccounts", table => table.HasCheckConstraint("CK_UserAccounts_Role", "[Role] IN (1, 2)"));
        users.HasKey(user => user.Id);
        users.Property(user => user.Id).UseIdentityColumn();
        users.Property(user => user.UserName).HasMaxLength(64).IsRequired();
        users.Property(user => user.NormalizedUserName).HasMaxLength(64)
            .UseCollation("Latin1_General_100_CI_AS").IsRequired();
        users.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
        users.Property(user => user.RowVersion).IsRowVersion();
        users.HasIndex(user => user.NormalizedUserName).IsUnique();
        users.HasIndex(user => new { user.Role, user.IsActive });
        var history = modelBuilder.Entity<SeedHistory>();
        history.ToTable("SeedHistory");
        history.HasKey(item => item.Id);
        history.Property(item => item.Id).UseIdentityColumn();
        history.Property(item => item.SeedVersion).HasMaxLength(32).IsRequired();
        history.Property(item => item.SeedUnitKey).HasMaxLength(64).IsRequired();
        history.HasIndex(item => new { item.SeedVersion, item.SeedUnitKey }).IsUnique();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderInventoryDbContext).Assembly);
    }
}
