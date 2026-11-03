using Microsoft.Extensions.Options;
using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Seeding;
using OrderInventory.Infrastructure.Configuration;
using OrderInventory.Infrastructure.Identity;

namespace OrderInventory.Infrastructure.Persistence;

public sealed class BootstrapSeeder(OrderInventoryDbContext context, IPasswordService passwords, TimeProvider clock,
    IOptions<BootstrapOptions> options)
{
    public Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var service = new BootstrapSeedService(new EfUserStore(context), new EfSeedHistoryStore(context), passwords, clock);
        return service.SeedAsync(new BootstrapAccounts(settings.AdminLogin, settings.AdminPassword,
            settings.StaffLogin, settings.StaffPassword), cancellationToken);
    }
}
