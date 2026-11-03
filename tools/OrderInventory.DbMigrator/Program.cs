using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrderInventory.Infrastructure;
using OrderInventory.Infrastructure.Configuration;
using OrderInventory.Infrastructure.Persistence;

try
{
    var configuration = new ConfigurationBuilder().AddUserSecrets("order-inventory-local")
        .AddEnvironmentVariables().Build();
    var services = new ServiceCollection();
    services.AddInfrastructure(configuration);
    await using var provider = services.BuildServiceProvider();
    _ = provider.GetRequiredService<IOptions<JwtOptions>>().Value;
    await using var scope = provider.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<OrderInventoryDbContext>();
    await context.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<BootstrapSeeder>().SeedAsync();
    Console.WriteLine("Database migration and bootstrap completed.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"Database initialization failed ({error.GetType().Name}). Configuration values were not logged.");
    return 1;
}
