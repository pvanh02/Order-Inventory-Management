using Microsoft.EntityFrameworkCore;
using OrderInventory.Application.Abstractions;

namespace OrderInventory.Infrastructure.Persistence;

public sealed class EfSeedHistoryStore(OrderInventoryDbContext context) : ISeedHistoryStore
{
    public Task<bool> ExistsAsync(string seedVersion, string seedUnitKey, CancellationToken cancellationToken) =>
        PersistenceErrors.ExecuteAsync(() => context.SeedHistory.AnyAsync(
            item => item.SeedVersion == seedVersion && item.SeedUnitKey == seedUnitKey, cancellationToken));

    public void Add(string seedVersion, string seedUnitKey, DateTimeOffset completedAt) =>
        context.SeedHistory.Add(new SeedHistory
        {
            SeedVersion = seedVersion,
            SeedUnitKey = seedUnitKey,
            CompletedAt = completedAt
        });
}
