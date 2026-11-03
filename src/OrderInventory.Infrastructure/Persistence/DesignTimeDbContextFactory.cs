using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OrderInventory.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrderInventoryDbContext>
{
    public OrderInventoryDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets("order-inventory-local")
            .AddEnvironmentVariables().Build();
        var connection = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("ConnectionStrings:Database is required for migration tooling.");
        return new OrderInventoryDbContext(new DbContextOptionsBuilder<OrderInventoryDbContext>().UseSqlServer(connection).Options);
    }
}
